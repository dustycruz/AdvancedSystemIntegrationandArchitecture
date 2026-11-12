using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Store.Contracts.Events;
using Store.Messaging.Configuration;
using Store.Messaging.Connection;
using Store.Messaging.Topology;

namespace Store.Messaging.Consuming;

/// <summary>
/// Background consumer for one durable queue with manual acknowledgement (autoAck: false).
/// <list type="bullet">
/// <item>Success: the message is acked.</item>
/// <item>Handler failure: the message is re-queued as a copy with an incremented x-attempt header and the
/// original is acked, after a delay that grows with the attempt.</item>
/// <item>Attempt limit reached, or a body that cannot be read: BasicNack(requeue: false) sends the message
/// through store.dlx to "&lt;queue&gt;.dlq", where it stays for inspection. Nothing is dropped silently.</item>
/// </list>
/// </summary>
public abstract class RabbitMqConsumerBase<TEvent> : BackgroundService where TEvent : IntegrationEvent
{
    private readonly IRabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly IEnumerable<IConsumeScopeProvider> _scopeProviders;
    private IChannel? _channel;

    protected RabbitMqConsumerBase(
        IRabbitMqConnection connection,
        IOptions<RabbitMqOptions> options,
        IEnumerable<IConsumeScopeProvider> scopeProviders,
        ILogger logger)
    {
        _connection = connection;
        _options = options.Value;
        _scopeProviders = scopeProviders;
        Logger = logger;
    }

    protected ILogger Logger { get; }

    /// <summary>Durable queue this consumer reads, e.g. "inventory.order-placed".</summary>
    public abstract string QueueName { get; }

    /// <summary>Routing key the queue is bound to on the events exchange.</summary>
    public virtual string RoutingKey => RoutingKeys.For<TEvent>();

    protected abstract Task HandleAsync(TEvent message, MessageContext context, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(publisherConfirms: false, stoppingToken);
        await RabbitMqTopology.DeclareConsumerQueueAsync(_channel, _options, QueueName, RoutingKey, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(delivery, stoppingToken);

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        Logger.LogInformation("Consuming {EventType} from queue {Queue} (routing key {RoutingKey}, dead-letter queue {DeadLetterQueue})",
            typeof(TEvent).Name, QueueName, RoutingKey, RabbitMqTopology.DeadLetterQueueFor(QueueName));

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Host is stopping.
        }
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var channel = _channel!;
        var headers = ReadHeaders(delivery.BasicProperties.Headers);
        var attempt = ReadAttempt(headers);

        TEvent? message;
        try
        {
            message = JsonSerializer.Deserialize<TEvent>(delivery.Body.Span, MessagingJson.Options);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Message {DeliveryTag} on {Queue} is not a valid {EventType}; sending it to the dead-letter queue",
                delivery.DeliveryTag, QueueName, typeof(TEvent).Name);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            return;
        }

        if (message is null)
        {
            Logger.LogError("Message {DeliveryTag} on {Queue} has an empty body; sending it to the dead-letter queue",
                delivery.DeliveryTag, QueueName);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            return;
        }

        var context = new MessageContext(message.MessageId, message.CorrelationId, QueueName,
            delivery.RoutingKey, attempt, delivery.Redelivered);

        var scopes = _scopeProviders.Select(provider => provider.BeginScope(context, headers)).ToList();
        try
        {
            using var messageScope = Logger.BeginScope(new Dictionary<string, object>
            {
                ["MessageId"] = message.MessageId,
                ["MessageType"] = typeof(TEvent).Name,
                ["OrderId"] = message.OrderId,
                ["Queue"] = QueueName
            });

            try
            {
                Logger.LogInformation("Received {EventType} {MessageId} for order {OrderId} (attempt {Attempt}/{MaxAttempts})",
                    typeof(TEvent).Name, message.MessageId, message.OrderId, attempt, _options.MaxDeliveryAttempts);

                await HandleAsync(message, context, stoppingToken);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                await HandleFailureAsync(channel, delivery, message, attempt, ex, stoppingToken);
            }
        }
        finally
        {
            foreach (var scope in scopes)
            {
                scope?.Dispose();
            }
        }
    }

    private async Task HandleFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        TEvent message,
        int attempt,
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (attempt >= _options.MaxDeliveryAttempts)
        {
            Logger.LogError(exception,
                "Handling {EventType} {MessageId} failed on the last attempt ({Attempt}/{MaxAttempts}); rejecting it to {DeadLetterQueue}",
                typeof(TEvent).Name, message.MessageId, attempt, _options.MaxDeliveryAttempts,
                RabbitMqTopology.DeadLetterQueueFor(QueueName));
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            return;
        }

        var delay = TimeSpan.FromMilliseconds(_options.RetryDelayMilliseconds * attempt);
        Logger.LogWarning(exception,
            "Handling {EventType} {MessageId} failed (attempt {Attempt}/{MaxAttempts}); retrying in {Delay}",
            typeof(TEvent).Name, message.MessageId, attempt, _options.MaxDeliveryAttempts, delay);

        await Task.Delay(delay, stoppingToken);

        var retryProperties = new BasicProperties(delivery.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(delivery.BasicProperties.Headers ?? new Dictionary<string, object?>())
            {
                [MessagingHeaders.Attempt] = attempt + 1,
                [MessagingHeaders.LastError] = Truncate(exception.Message, 500)
            }
        };

        // Re-queue a copy at the back of the same queue through the default exchange, then ack the original.
        await channel.BasicPublishAsync(string.Empty, QueueName, mandatory: false,
            basicProperties: retryProperties, body: delivery.Body, cancellationToken: stoppingToken);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
    }

    private static IReadOnlyDictionary<string, object?> ReadHeaders(IDictionary<string, object?>? headers)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (headers is null)
        {
            return result;
        }

        foreach (var (key, value) in headers)
        {
            // AMQP delivers string headers as UTF-8 byte arrays.
            result[key] = value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : value;
        }

        return result;
    }

    private static int ReadAttempt(IReadOnlyDictionary<string, object?> headers) =>
        headers.TryGetValue(MessagingHeaders.Attempt, out var value) && value is not null
            && int.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), out var attempt)
            && attempt > 0
            ? attempt
            : 1;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is { IsOpen: true })
        {
            await _channel.CloseAsync(cancellationToken);
        }

        _channel?.Dispose();
    }
}

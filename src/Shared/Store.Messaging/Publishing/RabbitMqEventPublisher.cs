using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Store.Contracts.Events;
using Store.Messaging.Configuration;
using Store.Messaging.Connection;
using Store.Messaging.Topology;

namespace Store.Messaging.Publishing;

/// <summary>
/// Publishes persistent JSON messages on a channel with publisher confirms and confirmation tracking:
/// <c>BasicPublishAsync</c> only completes once the broker has confirmed the message, and throws when it
/// is nacked or returned as unroutable (mandatory flag), so a message is never lost silently. Failed
/// publishes are retried with a growing delay on a fresh channel.
/// </summary>
public sealed class RabbitMqEventPublisher(
    IRabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    IEnumerable<IPublishPropertiesEnricher> enrichers,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly IPublishPropertiesEnricher[] _enrichers = enrichers.ToArray();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var routingKey = RoutingKeys.For(eventType);
        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, MessagingJson.Options);

        for (var attempt = 1; ; attempt++)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                var channel = await GetChannelAsync(cancellationToken);
                var properties = CreateProperties(integrationEvent, eventType);

                await channel.BasicPublishAsync(_options.ExchangeName, routingKey, mandatory: true,
                    basicProperties: properties, body: body, cancellationToken: cancellationToken);

                logger.LogInformation("Published {EventType} {MessageId} for order {OrderId} with routing key {RoutingKey} (confirmed)",
                    eventType.Name, integrationEvent.MessageId, integrationEvent.OrderId, routingKey);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await ResetChannelAsync();

                if (attempt >= _options.PublishRetryCount)
                {
                    logger.LogError(ex, "Publishing {EventType} {MessageId} failed after {Attempts} attempts",
                        eventType.Name, integrationEvent.MessageId, attempt);
                    throw new MessagePublishException(
                        $"The broker did not confirm {eventType.Name} {integrationEvent.MessageId} after {attempt} attempts.", ex);
                }

                var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                logger.LogWarning("Publishing {EventType} {MessageId} was not confirmed (attempt {Attempt}/{MaxAttempts}): {Error}. Retrying in {Delay}",
                    eventType.Name, integrationEvent.MessageId, attempt, _options.PublishRetryCount, ex.Message, delay);
                await Task.Delay(delay, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private BasicProperties CreateProperties(IntegrationEvent integrationEvent, Type eventType)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            MessageId = integrationEvent.MessageId.ToString("D"),
            CorrelationId = integrationEvent.CorrelationId,
            Type = eventType.Name,
            AppId = _options.ClientProvidedName,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAt.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                [MessagingHeaders.Attempt] = 1
            }
        };

        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(properties, integrationEvent);
        }

        return properties;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        _channel = await connection.CreateChannelAsync(publisherConfirms: true, cancellationToken);
        await RabbitMqTopology.DeclareExchangesAsync(_channel, _options, cancellationToken);
        return _channel;
    }

    private async Task ResetChannelAsync()
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            if (_channel.IsOpen)
            {
                await _channel.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Ignoring error while closing a failed publish channel");
        }

        _channel.Dispose();
        _channel = null;
    }

    public async ValueTask DisposeAsync()
    {
        await ResetChannelAsync();
        _gate.Dispose();
    }
}

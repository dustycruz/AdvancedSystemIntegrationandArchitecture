using RabbitMQ.Client;
using Store.Messaging.Configuration;

namespace Store.Messaging.Topology;

/// <summary>
/// Declares the exchanges and queues used by the platform. Every declaration is idempotent, so each
/// service declares what it needs at startup without depending on another service having run first.
/// </summary>
public static class RabbitMqTopology
{
    public const string DeadLetterQueueSuffix = ".dlq";

    /// <summary>Returns the dead-letter queue name for <paramref name="queueName"/>.</summary>
    public static string DeadLetterQueueFor(string queueName) => queueName + DeadLetterQueueSuffix;

    /// <summary>Declares the durable "store.events" topic exchange and the durable "store.dlx" direct exchange.</summary>
    public static async Task DeclareExchangesAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(options.DeadLetterExchangeName, ExchangeType.Direct, durable: true, autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Declares a durable consumer queue bound to <paramref name="routingKey"/> on the events exchange.
    /// Rejected messages go through the dead-letter exchange to "&lt;queue&gt;.dlq", which is declared and bound here too.
    /// </summary>
    public static async Task DeclareConsumerQueueAsync(
        IChannel channel,
        RabbitMqOptions options,
        string queueName,
        string routingKey,
        CancellationToken cancellationToken)
    {
        await DeclareExchangesAsync(channel, options, cancellationToken);

        var deadLetterQueue = DeadLetterQueueFor(queueName);
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(deadLetterQueue, options.DeadLetterExchangeName, routingKey: queueName,
            arguments: null, cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = queueName
        };
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName, options.ExchangeName, routingKey,
            arguments: null, cancellationToken: cancellationToken);
    }
}

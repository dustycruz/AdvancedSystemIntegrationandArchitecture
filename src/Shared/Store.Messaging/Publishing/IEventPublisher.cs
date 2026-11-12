using Store.Contracts.Events;

namespace Store.Messaging.Publishing;

public interface IEventPublisher
{
    /// <summary>
    /// Publishes <paramref name="integrationEvent"/> to the events exchange with its routing key and waits
    /// for the broker's publisher confirm. Throws <see cref="MessagePublishException"/> when the broker
    /// does not confirm the message after the configured retries.
    /// </summary>
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

using Store.Contracts.Events;

namespace Store.Messaging.Consuming;

/// <summary>
/// Handles one event type. Resolved from a new DI scope for every message, so it can use a scoped DbContext.
/// Throwing makes the consumer retry the message; after the last attempt it goes to the dead-letter queue.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent message, MessageContext context, CancellationToken cancellationToken);
}

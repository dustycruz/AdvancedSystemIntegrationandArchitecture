namespace Store.Messaging.Consuming;

/// <summary>Delivery details passed to an event handler alongside the message.</summary>
public sealed record MessageContext(
    Guid MessageId,
    string CorrelationId,
    string QueueName,
    string RoutingKey,
    int Attempt,
    bool Redelivered);

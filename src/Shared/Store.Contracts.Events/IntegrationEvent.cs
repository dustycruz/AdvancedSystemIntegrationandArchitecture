namespace Store.Contracts.Events;

/// <summary>
/// Common envelope fields carried by every integration event on the <c>store.events</c> exchange.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Unique id of this message. Consumers use it for idempotency (ProcessedMessages table).</summary>
    public required Guid MessageId { get; init; }

    /// <summary>X-Correlation-ID of the HTTP request that started the flow, carried end to end.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The order this event belongs to. Every event in the order saga is keyed by order.</summary>
    public required Guid OrderId { get; init; }

    /// <summary>When the publishing service produced the event (UTC).</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

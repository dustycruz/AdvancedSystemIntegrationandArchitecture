namespace Order.Api.Domain;

/// <summary>
/// Orchestration state for one order: which step the saga is on and what has to be undone if it fails.
/// Kept separately from the order so the customer-facing order stays simple.
/// </summary>
public sealed class OrderSagaState
{
    public Guid OrderId { get; set; }
    public SagaStep Step { get; set; }
    public bool StockReserved { get; set; }
    public bool PaymentCharged { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal? ChargedAmount { get; set; }
    public string? CompensationReason { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL xmin, so two saga events for the same order cannot overwrite each other.</summary>
    public uint Version { get; set; }
}

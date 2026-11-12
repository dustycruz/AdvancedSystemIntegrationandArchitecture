namespace Order.Api.Domain;

/// <summary>One entry of an order's timeline: the status it moved to, when, and why (for compensation steps).</summary>
public sealed class OrderStatusHistory
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

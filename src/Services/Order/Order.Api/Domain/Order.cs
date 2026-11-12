namespace Order.Api.Domain;

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>X-Correlation-ID of the request that placed the order; reused on every saga event.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];

    public bool IsFinal => Status is OrderStatus.Confirmed or OrderStatus.Cancelled;

    /// <summary>Moves the order to <paramref name="status"/> and appends a timeline entry.</summary>
    public void ChangeStatus(OrderStatus status, string? reason, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
        if (status == OrderStatus.Cancelled)
        {
            CancellationReason = reason;
        }

        StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            Status = status,
            Reason = reason,
            OccurredAt = now
        });
    }

    /// <summary>
    /// Records a compensation step on the timeline without changing the status
    /// (for example "Payment refunded" while the order stays Compensating).
    /// </summary>
    public void RecordStep(string reason, DateTimeOffset now)
    {
        UpdatedAt = now;
        StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            Status = Status,
            Reason = reason,
            OccurredAt = now
        });
    }
}

namespace Payment.Api.Domain;

/// <summary>The charge (and possible refund) for one order. One payment per order.</summary>
public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public string? RefundReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

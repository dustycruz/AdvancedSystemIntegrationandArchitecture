using System.ComponentModel.DataAnnotations;
using Payment.Api.Domain;

namespace Payment.Api.Contracts;

/// <summary>PaymentResponse in contracts/payment.v1.yaml.</summary>
public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    PaymentStatus Status,
    string? FailureReason,
    string? RefundReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PaymentResponse From(Domain.Payment payment) => new(
        payment.Id, payment.OrderId, payment.CustomerId, payment.Amount, payment.Status,
        payment.FailureReason, payment.RefundReason, payment.CreatedAt, payment.UpdatedAt);
}

/// <summary>PaymentFailureModeRequest in contracts/payment.v1.yaml.</summary>
public sealed record PaymentFailureModeRequest
{
    [Required]
    public PaymentFailureMode? Mode { get; init; }
}

/// <summary>PaymentFailureModeResponse in contracts/payment.v1.yaml.</summary>
public sealed record PaymentFailureModeResponse(PaymentFailureMode Mode, DateTimeOffset UpdatedAt)
{
    public static PaymentFailureModeResponse From(PaymentSettings settings) => new(settings.FailureMode, settings.UpdatedAt);
}

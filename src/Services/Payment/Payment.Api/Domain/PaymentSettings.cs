namespace Payment.Api.Domain;

/// <summary>Single-row settings table, so the failure mode survives restarts and is shared by all instances.</summary>
public sealed class PaymentSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public PaymentFailureMode FailureMode { get; set; } = PaymentFailureMode.None;
    public DateTimeOffset UpdatedAt { get; set; }
}

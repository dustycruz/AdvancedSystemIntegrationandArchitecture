namespace Store.Contracts.Events;

/// <summary>Command from the Order saga: charge the customer for the order.</summary>
[RoutingKey(RoutingKeys.ChargePaymentRequested)]
public sealed record ChargePaymentRequested : IntegrationEvent
{
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
}

/// <summary>Published by Payment when the charge succeeded.</summary>
[RoutingKey(RoutingKeys.PaymentCharged)]
public sealed record PaymentCharged : IntegrationEvent
{
    public required Guid PaymentId { get; init; }
    public required decimal Amount { get; init; }
}

/// <summary>Published by Payment when the charge was declined. Nothing was taken from the customer.</summary>
[RoutingKey(RoutingKeys.PaymentFailed)]
public sealed record PaymentFailed : IntegrationEvent
{
    public required string Reason { get; init; }
}

/// <summary>Compensation command from the Order saga: refund a charge that already succeeded.</summary>
[RoutingKey(RoutingKeys.RefundPaymentRequested)]
public sealed record RefundPaymentRequested : IntegrationEvent
{
    public required Guid PaymentId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
}

/// <summary>Published by Payment when the refund has been recorded.</summary>
[RoutingKey(RoutingKeys.PaymentRefunded)]
public sealed record PaymentRefunded : IntegrationEvent
{
    public required Guid PaymentId { get; init; }
    public required decimal Amount { get; init; }
}

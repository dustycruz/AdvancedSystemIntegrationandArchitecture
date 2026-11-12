namespace Order.Api.Domain;

/// <summary>
/// Happy path: Pending → StockReserved → PaymentCharged → Confirmed.
/// Compensation runs while the order is Compensating and always ends in Cancelled.
/// </summary>
public enum OrderStatus
{
    Pending,
    StockReserved,
    PaymentCharged,
    Compensating,
    Confirmed,
    Cancelled
}

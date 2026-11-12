namespace Store.Contracts.Events;

/// <summary>Published by Inventory when every line of an order has been reserved.</summary>
[RoutingKey(RoutingKeys.StockReserved)]
public sealed record StockReserved : IntegrationEvent
{
    public required IReadOnlyList<OrderLineItem> Lines { get; init; }
}

/// <summary>
/// Published by Inventory when at least one line could not be reserved. Nothing is held for the order.
/// </summary>
[RoutingKey(RoutingKeys.StockReservationFailed)]
public sealed record StockReservationFailed : IntegrationEvent
{
    public required string Reason { get; init; }
}

/// <summary>Compensation command from the Order saga: release everything reserved for the order.</summary>
[RoutingKey(RoutingKeys.StockReleaseRequested)]
public sealed record StockReleaseRequested : IntegrationEvent
{
    public required string Reason { get; init; }
}

/// <summary>Published by Inventory when the reservations of an order have been released.</summary>
[RoutingKey(RoutingKeys.StockReleased)]
public sealed record StockReleased : IntegrationEvent
{
    public required int ReleasedQuantity { get; init; }
}

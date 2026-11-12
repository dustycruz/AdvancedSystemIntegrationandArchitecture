namespace Store.Contracts.Events;

/// <summary>One line of an order, with the unit price snapshot taken from Catalog when the order was placed.</summary>
public sealed record OrderLineItem
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

/// <summary>
/// Published by Order when a customer places an order. Starts the saga: Inventory reserves the stock.
/// </summary>
[RoutingKey(RoutingKeys.OrderPlaced)]
public sealed record OrderPlaced : IntegrationEvent
{
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<OrderLineItem> Lines { get; init; }
}

/// <summary>
/// Published by Order when the saga completes successfully. Inventory turns the reservation into a sale.
/// </summary>
[RoutingKey(RoutingKeys.OrderConfirmed)]
public sealed record OrderConfirmed : IntegrationEvent
{
    public required decimal TotalAmount { get; init; }
}

/// <summary>Published by Order when the saga ends without a sale, after all compensation has finished.</summary>
[RoutingKey(RoutingKeys.OrderCancelled)]
public sealed record OrderCancelled : IntegrationEvent
{
    public required string Reason { get; init; }
}

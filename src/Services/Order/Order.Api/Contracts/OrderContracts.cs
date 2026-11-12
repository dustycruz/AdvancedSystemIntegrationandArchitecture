using System.ComponentModel.DataAnnotations;
using Order.Api.Domain;

namespace Order.Api.Contracts;

/// <summary>PlaceOrderLineRequest in contracts/order.v1.yaml.</summary>
public sealed record PlaceOrderLineRequest
{
    [Required]
    public Guid? ProductId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}

/// <summary>PlaceOrderRequest in contracts/order.v1.yaml.</summary>
public sealed record PlaceOrderRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public List<PlaceOrderLineRequest> Lines { get; init; } = [];
}

/// <summary>OrderLineResponse in contracts/order.v1.yaml.</summary>
public sealed record OrderLineResponse(
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

/// <summary>OrderStatusHistoryResponse in contracts/order.v1.yaml.</summary>
public sealed record OrderStatusHistoryResponse(OrderStatus Status, string? Reason, DateTimeOffset OccurredAt);

/// <summary>OrderResponse in contracts/order.v1.yaml.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    OrderStatus Status,
    decimal TotalAmount,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OrderLineResponse> Lines,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory)
{
    public static OrderResponse From(Domain.Order order) => new(
        order.Id,
        order.CustomerId,
        order.CustomerEmail,
        order.Status,
        order.TotalAmount,
        order.CancellationReason,
        order.CreatedAt,
        order.UpdatedAt,
        order.Lines
            .OrderBy(l => l.ProductName)
            .Select(l => new OrderLineResponse(l.ProductId, l.Sku, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList(),
        order.StatusHistory
            .OrderBy(h => h.OccurredAt)
            .ThenBy(h => h.Id)
            .Select(h => new OrderStatusHistoryResponse(h.Status, h.Reason, h.OccurredAt))
            .ToList());
}

/// <summary>OrderSummaryResponse in contracts/order.v1.yaml.</summary>
public sealed record OrderSummaryResponse(
    Guid Id,
    string CustomerEmail,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>OrderFailureModeRequest in contracts/order.v1.yaml.</summary>
public sealed record OrderFailureModeRequest
{
    [Required]
    public OrderFailureMode? Mode { get; init; }
}

/// <summary>OrderFailureModeResponse in contracts/order.v1.yaml.</summary>
public sealed record OrderFailureModeResponse(OrderFailureMode Mode, DateTimeOffset UpdatedAt)
{
    public static OrderFailureModeResponse From(OrderSettings settings) => new(settings.FailureMode, settings.UpdatedAt);
}

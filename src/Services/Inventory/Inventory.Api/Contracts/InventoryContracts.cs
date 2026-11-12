using System.ComponentModel.DataAnnotations;
using Inventory.Api.Domain;

namespace Inventory.Api.Contracts;

/// <summary>StockItemResponse in contracts/inventory.v1.yaml.</summary>
public sealed record StockItemResponse(
    Guid ProductId,
    string Sku,
    string ProductName,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    DateTimeOffset UpdatedAt)
{
    public static StockItemResponse From(StockItem item) => new(
        item.ProductId, item.Sku, item.ProductName, item.QuantityOnHand,
        item.QuantityReserved, item.QuantityAvailable, item.UpdatedAt);
}

/// <summary>StockAdjustmentRequest in contracts/inventory.v1.yaml.</summary>
public sealed record StockAdjustmentRequest
{
    /// <summary>Positive to add stock, negative to remove it. Zero is rejected.</summary>
    [Range(-100000, 100000)]
    public int QuantityDelta { get; init; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>ReservationResponse in contracts/inventory.v1.yaml.</summary>
public sealed record ReservationResponse(
    Guid Id,
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    ReservationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ReservationResponse From(StockReservation reservation) => new(
        reservation.Id, reservation.OrderId, reservation.ProductId, reservation.Quantity,
        reservation.Status, reservation.CreatedAt, reservation.UpdatedAt);
}

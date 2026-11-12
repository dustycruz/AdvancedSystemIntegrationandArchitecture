using Inventory.Api.Data;
using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Events;
using Store.Messaging.Consuming;
using Store.Messaging.Idempotency;

namespace Inventory.Api.Messaging;

/// <summary>
/// Safety net for the end of a cancelled saga: releases any reservation still held for the order.
/// In the normal flows the saga has already released stock (or never reserved it), so this finds nothing;
/// it guarantees a cancelled order can never keep stock locked. It also gives OrderCancelled a durable
/// subscriber, so the publisher's mandatory routing check holds for every event.
/// </summary>
public sealed class OrderCancelledHandler(
    InventoryDbContext db,
    TimeProvider timeProvider,
    ILogger<OrderCancelledHandler> logger) : IEventHandler<OrderCancelled>
{
    public async Task HandleAsync(OrderCancelled message, MessageContext context, CancellationToken cancellationToken)
    {
        await db.ProcessOnceAsync(context, nameof(OrderCancelled), async ct =>
        {
            var reservations = await db.Reservations
                .Where(r => r.OrderId == message.OrderId && r.Status == ReservationStatus.Reserved)
                .ToListAsync(ct);
            if (reservations.Count == 0)
            {
                logger.LogInformation("Order {OrderId} was cancelled ({Reason}); no stock is held for it",
                    message.OrderId, message.Reason);
                return;
            }

            var now = timeProvider.GetUtcNow();
            var productIds = reservations.Select(r => r.ProductId).Distinct().ToList();
            var stock = await db.StockItems.Where(s => productIds.Contains(s.ProductId)).ToDictionaryAsync(s => s.ProductId, ct);
            foreach (var reservation in reservations)
            {
                stock[reservation.ProductId].QuantityReserved -= reservation.Quantity;
                stock[reservation.ProductId].UpdatedAt = now;
                reservation.Status = ReservationStatus.Released;
                reservation.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);
            logger.LogWarning("Released {Units} units still held for cancelled order {OrderId}",
                reservations.Sum(r => r.Quantity), message.OrderId);
        }, cancellationToken);
    }
}

using Inventory.Api.Data;
using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Events;
using Store.Messaging.Consuming;
using Store.Messaging.Idempotency;

namespace Inventory.Api.Messaging;

/// <summary>
/// Final step of a successful saga: the reserved units are sold, so they leave both the reserved and the
/// on-hand quantities and the reservations become Committed.
/// </summary>
public sealed class OrderConfirmedHandler(
    InventoryDbContext db,
    TimeProvider timeProvider,
    ILogger<OrderConfirmedHandler> logger) : IEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(OrderConfirmed message, MessageContext context, CancellationToken cancellationToken)
    {
        await db.ProcessOnceAsync(context, nameof(OrderConfirmed), async ct =>
        {
            var now = timeProvider.GetUtcNow();
            var reservations = await db.Reservations
                .Where(r => r.OrderId == message.OrderId && r.Status == ReservationStatus.Reserved)
                .ToListAsync(ct);

            var productIds = reservations.Select(r => r.ProductId).Distinct().ToList();
            var stock = await db.StockItems.Where(s => productIds.Contains(s.ProductId)).ToDictionaryAsync(s => s.ProductId, ct);

            foreach (var reservation in reservations)
            {
                var item = stock[reservation.ProductId];
                item.QuantityReserved -= reservation.Quantity;
                item.QuantityOnHand -= reservation.Quantity;
                item.UpdatedAt = now;
                reservation.Status = ReservationStatus.Committed;
                reservation.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Committed {Units} units for confirmed order {OrderId}",
                reservations.Sum(r => r.Quantity), message.OrderId);
        }, cancellationToken);
    }
}

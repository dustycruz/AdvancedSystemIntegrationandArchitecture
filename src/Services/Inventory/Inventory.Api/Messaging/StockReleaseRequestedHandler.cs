using Inventory.Api.Data;
using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Messaging.Consuming;
using Store.Messaging.Idempotency;
using Store.Messaging.Publishing;

namespace Inventory.Api.Messaging;

/// <summary>
/// Compensation: give back everything still reserved for the order and publish StockReleased.
/// StockReleased is published even when nothing was held, so the saga can always finish cancelling.
/// </summary>
public sealed class StockReleaseRequestedHandler(
    InventoryDbContext db,
    IEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<StockReleaseRequestedHandler> logger) : IEventHandler<StockReleaseRequested>
{
    public async Task HandleAsync(StockReleaseRequested message, MessageContext context, CancellationToken cancellationToken)
    {
        var processed = await db.ProcessOnceAsync(context, nameof(StockReleaseRequested), async ct =>
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
                item.UpdatedAt = now;
                reservation.Status = ReservationStatus.Released;
                reservation.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);

            var released = reservations.Sum(r => r.Quantity);
            await publisher.PublishAsync(new StockReleased
            {
                MessageId = MessageIds.Derive(context.MessageId, nameof(StockReleased)),
                CorrelationId = context.CorrelationId,
                OrderId = message.OrderId,
                OccurredAt = now,
                ReleasedQuantity = released
            }, ct);

            logger.LogInformation("Released {Units} reserved units for order {OrderId} ({Reason})",
                released, message.OrderId, message.Reason);
        }, cancellationToken);

        if (!processed)
        {
            logger.LogInformation("StockReleaseRequested {MessageId} was already processed; skipping", context.MessageId);
        }
    }
}

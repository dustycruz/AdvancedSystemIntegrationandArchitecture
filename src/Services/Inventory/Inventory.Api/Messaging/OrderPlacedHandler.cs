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
/// Saga step 1: reserve stock for every line of a new order, all or nothing. Publishes StockReserved,
/// or StockReservationFailed with the reason when any line cannot be covered (nothing is held then).
/// Reservation, ProcessedMessages row and outgoing event id are tied to the incoming message, so a
/// redelivery never reserves twice.
/// </summary>
public sealed class OrderPlacedHandler(
    InventoryDbContext db,
    IEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<OrderPlacedHandler> logger) : IEventHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced message, MessageContext context, CancellationToken cancellationToken)
    {
        var processed = await db.ProcessOnceAsync(context, nameof(OrderPlaced), async ct =>
        {
            var now = timeProvider.GetUtcNow();
            var requested = message.Lines
                .GroupBy(l => l.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => l.Quantity)))
                .ToList();
            var productIds = requested.Select(r => r.ProductId).ToList();
            var stock = await db.StockItems.Where(s => productIds.Contains(s.ProductId)).ToDictionaryAsync(s => s.ProductId, ct);

            var shortages = new List<string>();
            foreach (var (productId, quantity) in requested)
            {
                if (!stock.TryGetValue(productId, out var item))
                {
                    shortages.Add($"product {productId} is not stocked");
                }
                else if (item.QuantityAvailable < quantity)
                {
                    shortages.Add($"{item.ProductName}: requested {quantity}, available {item.QuantityAvailable}");
                }
            }

            if (shortages.Count > 0)
            {
                var reason = "Insufficient stock - " + string.Join("; ", shortages);
                logger.LogWarning("Cannot reserve stock for order {OrderId}: {Reason}", message.OrderId, reason);
                await publisher.PublishAsync(new StockReservationFailed
                {
                    MessageId = MessageIds.Derive(context.MessageId, nameof(StockReservationFailed)),
                    CorrelationId = context.CorrelationId,
                    OrderId = message.OrderId,
                    OccurredAt = now,
                    Reason = reason
                }, ct);
                return;
            }

            foreach (var (productId, quantity) in requested)
            {
                var item = stock[productId];
                item.QuantityReserved += quantity;
                item.UpdatedAt = now;
                db.Reservations.Add(new StockReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = message.OrderId,
                    ProductId = productId,
                    Quantity = quantity,
                    Status = ReservationStatus.Reserved,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            // Saves the reservation; a concurrent change to the same stock row (xmin) throws here and the
            // consumer retries the whole message in a new transaction.
            await db.SaveChangesAsync(ct);

            await publisher.PublishAsync(new StockReserved
            {
                MessageId = MessageIds.Derive(context.MessageId, nameof(StockReserved)),
                CorrelationId = context.CorrelationId,
                OrderId = message.OrderId,
                OccurredAt = now,
                Lines = message.Lines
            }, ct);

            logger.LogInformation("Reserved {Units} units across {Products} products for order {OrderId}",
                requested.Sum(r => r.Quantity), requested.Count, message.OrderId);
        }, cancellationToken);

        if (!processed)
        {
            logger.LogInformation("OrderPlaced {MessageId} was already processed; skipping", context.MessageId);
        }
    }
}

using Order.Api.Clients.Catalog;
using Order.Api.Contracts;
using Order.Api.Data;
using Order.Api.Domain;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Messaging.Publishing;
using Store.Observability.Correlation;
using Store.Observability.ProblemDetails;

namespace Order.Api.Services;

/// <summary>
/// Places an order: validates every product with Catalog over HTTP, snapshots the current prices onto the
/// lines, stores the order as Pending with its saga state, and publishes OrderPlaced to start the saga.
/// The order row and the publish happen in one database transaction: if the broker does not confirm
/// OrderPlaced, the transaction is rolled back and the caller gets 503, so no order is left without a saga.
/// </summary>
public sealed class OrderPlacementService(
    OrderDbContext db,
    ICatalogProductLookup catalog,
    IEventPublisher publisher,
    ICorrelationIdAccessor correlation,
    TimeProvider timeProvider,
    ILogger<OrderPlacementService> logger)
{
    private const int MaxQuantityPerProduct = 100;

    public async Task<Domain.Order> PlaceOrderAsync(
        Guid customerId,
        string customerEmail,
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var requested = request.Lines
            .GroupBy(l => l.ProductId!.Value)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => l.Quantity)))
            .ToList();

        var tooMany = requested.FirstOrDefault(l => l.Quantity > MaxQuantityPerProduct);
        if (tooMany != default)
        {
            throw new BadRequestException(
                $"At most {MaxQuantityPerProduct} units of one product can be ordered; product {tooMany.ProductId} has {tooMany.Quantity}.");
        }

        var orderId = Guid.NewGuid();
        var lines = new List<OrderLine>();
        foreach (var (productId, quantity) in requested)
        {
            var lookup = await catalog.GetProductAsync(productId, cancellationToken);
            if (!lookup.IsFound)
            {
                throw new NotFoundException($"Product {productId} does not exist.");
            }

            var product = lookup.Product!;
            if (!product.IsActive)
            {
                throw new ConflictException($"{product.Name} is no longer for sale.");
            }

            lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = product.Id,
                Sku = product.Sku,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }

        var now = timeProvider.GetUtcNow();
        var correlationId = correlation.GetOrCreate();
        var order = new Domain.Order
        {
            Id = orderId,
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            TotalAmount = lines.Sum(l => l.LineTotal),
            CorrelationId = correlationId,
            CreatedAt = now,
            Lines = lines
        };
        order.ChangeStatus(OrderStatus.Pending, "Order placed; reserving stock", now);

        db.Orders.Add(order);
        db.SagaStates.Add(new OrderSagaState
        {
            OrderId = orderId,
            Step = SagaStep.AwaitingStockReservation,
            StartedAt = now,
            UpdatedAt = now
        });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await publisher.PublishAsync(new OrderPlaced
            {
                MessageId = MessageIds.New(),
                CorrelationId = correlationId,
                OrderId = orderId,
                OccurredAt = now,
                CustomerId = customerId,
                TotalAmount = order.TotalAmount,
                Lines = lines.Select(l => new OrderLineItem
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            }, cancellationToken);
        }
        catch (MessagePublishException ex)
        {
            throw new DownstreamServiceException("message broker",
                "The order could not be submitted because the message broker is unavailable. Nothing was charged; please try again.", ex);
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Order {OrderId} placed by {CustomerId}: {LineCount} lines, total {Total}",
            orderId, customerId, lines.Count, order.TotalAmount);
        return order;
    }
}

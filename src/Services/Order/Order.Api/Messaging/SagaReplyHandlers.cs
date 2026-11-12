using Order.Api.Saga;
using Store.Contracts.Events;
using Store.Messaging.Consuming;

namespace Order.Api.Messaging;

// Thin consumer handlers: each saga reply arriving on its own queue is passed to the orchestrator.
// They are resolved per message from a new DI scope, so each gets its own DbContext and transaction.

public sealed class StockReservedHandler(OrderSagaOrchestrator saga) : IEventHandler<StockReserved>
{
    public Task HandleAsync(StockReserved message, MessageContext context, CancellationToken cancellationToken) =>
        saga.HandleAsync(message, context, cancellationToken);
}

public sealed class StockReservationFailedHandler(OrderSagaOrchestrator saga) : IEventHandler<StockReservationFailed>
{
    public Task HandleAsync(StockReservationFailed message, MessageContext context, CancellationToken cancellationToken) =>
        saga.HandleAsync(message, context, cancellationToken);
}

public sealed class StockReleasedHandler(OrderSagaOrchestrator saga) : IEventHandler<StockReleased>
{
    public Task HandleAsync(StockReleased message, MessageContext context, CancellationToken cancellationToken) =>
        saga.HandleAsync(message, context, cancellationToken);
}

public sealed class PaymentChargedHandler(OrderSagaOrchestrator saga) : IEventHandler<PaymentCharged>
{
    public Task HandleAsync(PaymentCharged message, MessageContext context, CancellationToken cancellationToken) =>
        saga.HandleAsync(message, context, cancellationToken);
}

public sealed class PaymentFailedHandler(OrderSagaOrchestrator saga) : IEventHandler<PaymentFailed>
{
    public Task HandleAsync(PaymentFailed message, MessageContext context, CancellationToken cancellationToken) =>
        saga.HandleAsync(message, context, cancellationToken);
}

public sealed class PaymentRefundedHandler(OrderSagaOrchestrator saga) : IEventHandler<PaymentRefunded>
{
    public Task HandleAsync(PaymentRefunded message, MessageContext context, CancellationToken cancellationToken) =>
        saga.HandleAsync(message, context, cancellationToken);
}

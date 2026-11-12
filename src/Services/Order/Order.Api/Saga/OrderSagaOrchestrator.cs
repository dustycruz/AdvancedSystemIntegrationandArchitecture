using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Order.Api.Data;
using Order.Api.Domain;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Messaging.Consuming;
using Store.Messaging.Idempotency;
using Store.Messaging.Publishing;

namespace Order.Api.Saga;

/// <summary>
/// Orchestrates the order saga. Each reply event is handled as one local transaction in the Order database:
/// the order status and history, the saga state, the processed_messages record and the next command or
/// event (published with confirms before commit, with an id derived from the incoming message).
/// <code>
/// Happy path:   Pending → StockReserved → PaymentCharged → Confirmed
/// No stock:     Pending → Cancelled (nothing charged)
/// Declined:     StockReserved → Compensating → release stock → Cancelled
/// After charge: PaymentCharged → Compensating → refund → release stock → Cancelled
/// </code>
/// A reply that does not match the step the saga is waiting for (a duplicate, or one that arrives after
/// the order already finished) is acknowledged and ignored, so the saga never moves backwards.
/// </summary>
public sealed class OrderSagaOrchestrator(
    OrderDbContext db,
    IEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<OrderSagaOrchestrator> logger)
{
    public Task HandleAsync(StockReserved message, MessageContext context, CancellationToken cancellationToken) =>
        RunStepAsync(message, context, SagaStep.AwaitingStockReservation, async (order, saga, now, ct) =>
        {
            saga.StockReserved = true;
            saga.Step = SagaStep.AwaitingPayment;
            order.ChangeStatus(OrderStatus.StockReserved,
                $"Stock reserved for {order.Lines.Sum(l => l.Quantity)} item(s); charging payment", now);

            await publisher.PublishAsync(new ChargePaymentRequested
            {
                MessageId = MessageIds.Derive(context.MessageId, nameof(ChargePaymentRequested)),
                CorrelationId = context.CorrelationId,
                OrderId = order.Id,
                OccurredAt = now,
                CustomerId = order.CustomerId,
                Amount = order.TotalAmount
            }, ct);
        }, cancellationToken);

    public Task HandleAsync(StockReservationFailed message, MessageContext context, CancellationToken cancellationToken) =>
        RunStepAsync(message, context, SagaStep.AwaitingStockReservation, async (order, saga, now, ct) =>
        {
            saga.Step = SagaStep.Completed;
            saga.CompensationReason = message.Reason;
            order.ChangeStatus(OrderStatus.Cancelled, $"{message.Reason}. Nothing was charged.", now);
            await PublishCancelledAsync(order, context, now, ct);
        }, cancellationToken);

    public Task HandleAsync(PaymentCharged message, MessageContext context, CancellationToken cancellationToken) =>
        RunStepAsync(message, context, SagaStep.AwaitingPayment, async (order, saga, now, ct) =>
        {
            saga.PaymentCharged = true;
            saga.PaymentId = message.PaymentId;
            saga.ChargedAmount = message.Amount;
            order.ChangeStatus(OrderStatus.PaymentCharged, $"Payment of {FormatAmount(message.Amount)} charged", now);

            var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
            if (settings?.FailureMode == OrderFailureMode.FailAfterPayment)
            {
                const string reason = "Order processing failed after payment (failure mode FailAfterPayment is on)";
                logger.LogWarning("Simulated failure after payment for order {OrderId}; starting compensation", order.Id);

                saga.Step = SagaStep.AwaitingRefund;
                saga.CompensationReason = reason;
                order.ChangeStatus(OrderStatus.Compensating, $"{reason}; refunding payment", now);

                await publisher.PublishAsync(new RefundPaymentRequested
                {
                    MessageId = MessageIds.Derive(context.MessageId, nameof(RefundPaymentRequested)),
                    CorrelationId = context.CorrelationId,
                    OrderId = order.Id,
                    OccurredAt = now,
                    PaymentId = message.PaymentId,
                    Amount = message.Amount,
                    Reason = reason
                }, ct);
                return;
            }

            saga.Step = SagaStep.Completed;
            order.ChangeStatus(OrderStatus.Confirmed, "Order confirmed", now);

            await publisher.PublishAsync(new OrderConfirmed
            {
                MessageId = MessageIds.Derive(context.MessageId, nameof(OrderConfirmed)),
                CorrelationId = context.CorrelationId,
                OrderId = order.Id,
                OccurredAt = now,
                TotalAmount = order.TotalAmount
            }, ct);
        }, cancellationToken);

    public Task HandleAsync(PaymentFailed message, MessageContext context, CancellationToken cancellationToken) =>
        RunStepAsync(message, context, SagaStep.AwaitingPayment, async (order, saga, now, ct) =>
        {
            var reason = $"Payment declined: {message.Reason}";
            saga.Step = SagaStep.AwaitingStockRelease;
            saga.CompensationReason = reason;
            order.ChangeStatus(OrderStatus.Compensating, $"{reason}; releasing reserved stock", now);
            await RequestStockReleaseAsync(order, context, reason, now, ct);
        }, cancellationToken);

    public Task HandleAsync(PaymentRefunded message, MessageContext context, CancellationToken cancellationToken) =>
        RunStepAsync(message, context, SagaStep.AwaitingRefund, async (order, saga, now, ct) =>
        {
            saga.Step = SagaStep.AwaitingStockRelease;
            order.RecordStep($"Payment of {FormatAmount(message.Amount)} refunded; releasing reserved stock", now);
            await RequestStockReleaseAsync(order, context, saga.CompensationReason ?? "Order cancelled", now, ct);
        }, cancellationToken);

    public Task HandleAsync(StockReleased message, MessageContext context, CancellationToken cancellationToken) =>
        RunStepAsync(message, context, SagaStep.AwaitingStockRelease, async (order, saga, now, ct) =>
        {
            saga.Step = SagaStep.Completed;
            saga.StockReserved = false;
            order.RecordStep($"Reserved stock released ({message.ReleasedQuantity} item(s))", now);

            var reason = saga.CompensationReason ?? "Order cancelled";
            order.ChangeStatus(OrderStatus.Cancelled,
                saga.PaymentCharged ? $"{reason}. The payment was refunded." : $"{reason}. Nothing was charged.", now);
            await PublishCancelledAsync(order, context, now, ct);
        }, cancellationToken);

    private async Task RunStepAsync(
        IntegrationEvent message,
        MessageContext context,
        SagaStep expectedStep,
        Func<Domain.Order, OrderSagaState, DateTimeOffset, CancellationToken, Task> step,
        CancellationToken cancellationToken)
    {
        var messageType = message.GetType().Name;
        var processed = await db.ProcessOnceAsync(context, messageType, async ct =>
        {
            var order = await db.Orders
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == message.OrderId, ct);
            var saga = await db.SagaStates.FirstOrDefaultAsync(s => s.OrderId == message.OrderId, ct);

            if (order is null || saga is null)
            {
                logger.LogWarning("{MessageType} refers to unknown order {OrderId}; ignoring it", messageType, message.OrderId);
                return;
            }

            if (saga.Step != expectedStep)
            {
                logger.LogInformation("Ignoring {MessageType} for order {OrderId}: saga is at {Step}, expected {ExpectedStep}",
                    messageType, order.Id, saga.Step, expectedStep);
                return;
            }

            var now = timeProvider.GetUtcNow();
            var previousStatus = order.Status;
            await step(order, saga, now, ct);
            saga.UpdatedAt = now;

            // Status, history and saga state are saved before the transaction commits; ProcessOnceAsync
            // then records the message and commits everything together.
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Saga for order {OrderId}: {MessageType} moved {PreviousStatus} -> {Status} (next: {Step})",
                order.Id, messageType, previousStatus, order.Status, saga.Step);
        }, cancellationToken);

        if (!processed)
        {
            logger.LogInformation("{MessageType} {MessageId} was already processed; skipping", messageType, context.MessageId);
        }
    }

    private Task RequestStockReleaseAsync(Domain.Order order, MessageContext context, string reason, DateTimeOffset now, CancellationToken ct) =>
        publisher.PublishAsync(new StockReleaseRequested
        {
            MessageId = MessageIds.Derive(context.MessageId, nameof(StockReleaseRequested)),
            CorrelationId = context.CorrelationId,
            OrderId = order.Id,
            OccurredAt = now,
            Reason = reason
        }, ct);

    private Task PublishCancelledAsync(Domain.Order order, MessageContext context, DateTimeOffset now, CancellationToken ct) =>
        publisher.PublishAsync(new OrderCancelled
        {
            MessageId = MessageIds.Derive(context.MessageId, nameof(OrderCancelled)),
            CorrelationId = context.CorrelationId,
            OrderId = order.Id,
            OccurredAt = now,
            Reason = order.CancellationReason ?? "Order cancelled"
        }, ct);

    private static string FormatAmount(decimal amount) =>
        "PHP " + amount.ToString("N2", CultureInfo.InvariantCulture);
}

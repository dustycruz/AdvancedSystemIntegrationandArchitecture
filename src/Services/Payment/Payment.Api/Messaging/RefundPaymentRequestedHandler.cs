using Microsoft.EntityFrameworkCore;
using Payment.Api.Data;
using Payment.Api.Domain;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Messaging.Consuming;
using Store.Messaging.Idempotency;
using Store.Messaging.Publishing;

namespace Payment.Api.Messaging;

/// <summary>
/// Compensation: refund a charge that already succeeded and publish PaymentRefunded. A refund request for a
/// payment that does not exist is an inconsistency, so it throws; after the retries the message is kept in
/// payment.refund-payment-requested.dlq for investigation instead of being dropped.
/// </summary>
public sealed class RefundPaymentRequestedHandler(
    PaymentDbContext db,
    IEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<RefundPaymentRequestedHandler> logger) : IEventHandler<RefundPaymentRequested>
{
    public async Task HandleAsync(RefundPaymentRequested message, MessageContext context, CancellationToken cancellationToken)
    {
        var processed = await db.ProcessOnceAsync(context, nameof(RefundPaymentRequested), async ct =>
        {
            var now = timeProvider.GetUtcNow();
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == message.PaymentId && p.OrderId == message.OrderId, ct)
                ?? throw new InvalidOperationException(
                    $"Cannot refund payment {message.PaymentId} for order {message.OrderId}: no such payment exists.");

            if (payment.Status == PaymentStatus.Charged)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundReason = message.Reason;
                payment.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                logger.LogWarning("Refunded {Amount} for order {OrderId} (payment {PaymentId}): {Reason}",
                    payment.Amount, message.OrderId, payment.Id, message.Reason);
            }
            else if (payment.Status != PaymentStatus.Refunded)
            {
                throw new InvalidOperationException(
                    $"Cannot refund payment {payment.Id} for order {message.OrderId}: its status is {payment.Status}.");
            }

            await publisher.PublishAsync(new PaymentRefunded
            {
                MessageId = MessageIds.Derive(context.MessageId, nameof(PaymentRefunded)),
                CorrelationId = context.CorrelationId,
                OrderId = message.OrderId,
                OccurredAt = now,
                PaymentId = payment.Id,
                Amount = payment.Amount
            }, ct);
        }, cancellationToken);

        if (!processed)
        {
            logger.LogInformation("RefundPaymentRequested {MessageId} was already processed; skipping", context.MessageId);
        }
    }
}

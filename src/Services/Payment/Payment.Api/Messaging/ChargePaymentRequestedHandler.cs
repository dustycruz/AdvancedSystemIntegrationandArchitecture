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
/// Saga step 2: charge the customer. With failure mode DeclineCharge the charge is recorded as Declined and
/// PaymentFailed is published; otherwise it is recorded as Charged and PaymentCharged is published.
/// </summary>
public sealed class ChargePaymentRequestedHandler(
    PaymentDbContext db,
    IEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<ChargePaymentRequestedHandler> logger) : IEventHandler<ChargePaymentRequested>
{
    public async Task HandleAsync(ChargePaymentRequested message, MessageContext context, CancellationToken cancellationToken)
    {
        var processed = await db.ProcessOnceAsync(context, nameof(ChargePaymentRequested), async ct =>
        {
            var now = timeProvider.GetUtcNow();
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == message.OrderId, ct);

            if (payment is null)
            {
                var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
                var declined = settings?.FailureMode == PaymentFailureMode.DeclineCharge;

                payment = new Domain.Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = message.OrderId,
                    CustomerId = message.CustomerId,
                    Amount = message.Amount,
                    Status = declined ? PaymentStatus.Declined : PaymentStatus.Charged,
                    FailureReason = declined ? "Card declined (failure mode DeclineCharge is on)" : null,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Payments.Add(payment);
                await db.SaveChangesAsync(ct);
            }

            if (payment.Status == PaymentStatus.Declined)
            {
                logger.LogWarning("Declined payment of {Amount} for order {OrderId}: {Reason}",
                    payment.Amount, message.OrderId, payment.FailureReason);
                await publisher.PublishAsync(new PaymentFailed
                {
                    MessageId = MessageIds.Derive(context.MessageId, nameof(PaymentFailed)),
                    CorrelationId = context.CorrelationId,
                    OrderId = message.OrderId,
                    OccurredAt = now,
                    Reason = payment.FailureReason ?? "Payment declined"
                }, ct);
                return;
            }

            logger.LogInformation("Charged {Amount} for order {OrderId} (payment {PaymentId})",
                payment.Amount, message.OrderId, payment.Id);
            await publisher.PublishAsync(new PaymentCharged
            {
                MessageId = MessageIds.Derive(context.MessageId, nameof(PaymentCharged)),
                CorrelationId = context.CorrelationId,
                OrderId = message.OrderId,
                OccurredAt = now,
                PaymentId = payment.Id,
                Amount = payment.Amount
            }, ct);
        }, cancellationToken);

        if (!processed)
        {
            logger.LogInformation("ChargePaymentRequested {MessageId} was already processed; skipping", context.MessageId);
        }
    }
}

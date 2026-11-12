namespace Payment.Api.Messaging;

/// <summary>Durable queues owned by Payment. Each has a matching "&lt;queue&gt;.dlq" dead-letter queue.</summary>
public static class PaymentQueues
{
    public const string ChargePaymentRequested = "payment.charge-payment-requested";
    public const string RefundPaymentRequested = "payment.refund-payment-requested";
}

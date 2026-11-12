namespace Order.Api.Messaging;

/// <summary>Durable queues owned by Order (saga replies). Each has a matching "&lt;queue&gt;.dlq" dead-letter queue.</summary>
public static class OrderQueues
{
    public const string StockReserved = "order.stock-reserved";
    public const string StockReservationFailed = "order.stock-reservation-failed";
    public const string StockReleased = "order.stock-released";
    public const string PaymentCharged = "order.payment-charged";
    public const string PaymentFailed = "order.payment-failed";
    public const string PaymentRefunded = "order.payment-refunded";
}

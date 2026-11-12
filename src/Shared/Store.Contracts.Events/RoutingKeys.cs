using System.Reflection;

namespace Store.Contracts.Events;

/// <summary>
/// Routing keys for every integration event. Queues bind to these keys on the <c>store.events</c> exchange.
/// </summary>
public static class RoutingKeys
{
    public const string OrderPlaced = "order.placed";
    public const string OrderConfirmed = "order.confirmed";
    public const string OrderCancelled = "order.cancelled";

    public const string StockReserved = "stock.reserved";
    public const string StockReservationFailed = "stock.reservation-failed";
    public const string StockReleaseRequested = "stock.release-requested";
    public const string StockReleased = "stock.released";

    public const string ChargePaymentRequested = "payment.charge-requested";
    public const string PaymentCharged = "payment.charged";
    public const string PaymentFailed = "payment.failed";
    public const string RefundPaymentRequested = "payment.refund-requested";
    public const string PaymentRefunded = "payment.refunded";

    /// <summary>Returns the routing key declared on <typeparamref name="TEvent"/> with <see cref="RoutingKeyAttribute"/>.</summary>
    public static string For<TEvent>() where TEvent : IntegrationEvent => For(typeof(TEvent));

    /// <summary>Returns the routing key declared on <paramref name="eventType"/> with <see cref="RoutingKeyAttribute"/>.</summary>
    public static string For(Type eventType)
    {
        var attribute = eventType.GetCustomAttribute<RoutingKeyAttribute>()
            ?? throw new InvalidOperationException(
                $"{eventType.Name} has no [RoutingKey] attribute, so it cannot be published to store.events.");
        return attribute.RoutingKey;
    }
}

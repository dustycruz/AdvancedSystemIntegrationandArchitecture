namespace Store.Contracts.Events;

/// <summary>
/// Declares the routing key an event is published with on the <c>store.events</c> topic exchange.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RoutingKeyAttribute(string routingKey) : Attribute
{
    public string RoutingKey { get; } = routingKey;
}

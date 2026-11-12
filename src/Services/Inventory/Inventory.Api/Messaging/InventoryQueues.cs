namespace Inventory.Api.Messaging;

/// <summary>Durable queues owned by Inventory. Each has a matching "&lt;queue&gt;.dlq" dead-letter queue.</summary>
public static class InventoryQueues
{
    public const string OrderPlaced = "inventory.order-placed";
    public const string StockReleaseRequested = "inventory.stock-release-requested";
    public const string OrderConfirmed = "inventory.order-confirmed";
    public const string OrderCancelled = "inventory.order-cancelled";
}

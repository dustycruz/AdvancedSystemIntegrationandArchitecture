namespace Inventory.Api.Domain;

/// <summary>Stock for one product. Product id, SKU and name come from Catalog; quantities are owned here.</summary>
public sealed class StockItem
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Physical units, including units that are reserved for in-flight orders.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Units held for orders that are not confirmed or cancelled yet.</summary>
    public int QuantityReserved { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>PostgreSQL xmin, used as an optimistic concurrency token for concurrent reservations.</summary>
    public uint Version { get; set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;
}

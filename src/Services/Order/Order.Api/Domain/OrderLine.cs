namespace Order.Api.Domain;

/// <summary>
/// One product in an order. SKU, name and unit price are snapshots taken from Catalog when the order was
/// placed, so later catalog price changes never alter an existing order.
/// </summary>
public sealed class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

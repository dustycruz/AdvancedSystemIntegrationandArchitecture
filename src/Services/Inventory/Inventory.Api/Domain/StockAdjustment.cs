namespace Inventory.Api.Domain;

/// <summary>Audit record of a manual stock change made by an administrator.</summary>
public sealed class StockAdjustment
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityDelta { get; set; }
    public int QuantityOnHandAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdjustedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

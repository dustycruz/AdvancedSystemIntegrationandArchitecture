using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Data;

/// <summary>
/// Opening stock for the demo products. Ids, SKUs and names match the Catalog seed (Catalog owns those
/// values; they are copied here because each service has its own database). AC-JACKET-VAR has exactly
/// one unit so ordering two of them demonstrates the insufficient-stock cancellation.
/// </summary>
public static class InventorySeedData
{
    private static readonly (string Id, string Sku, string Name, int Quantity)[] OpeningStock =
    [
        ("a1000000-0000-0000-0000-000000000001", "AC-HOODIE-NVY", "Angelicum Hoodie - Navy", 25),
        ("a1000000-0000-0000-0000-000000000002", "AC-TSHIRT-WHT", "Classic Campus Tee - White", 40),
        ("a1000000-0000-0000-0000-000000000003", "AC-LANYARD-GLD", "Gold Lanyard and ID Holder", 100),
        ("a1000000-0000-0000-0000-000000000004", "AC-TUMBLER-BLU", "Insulated Tumbler 500 ml", 30),
        ("a1000000-0000-0000-0000-000000000005", "AC-NOTEBOOK-A5", "A5 Dotted Notebook", 60),
        ("a1000000-0000-0000-0000-000000000006", "AC-TOTE-CNV", "Canvas Tote Bag", 35),
        ("a1000000-0000-0000-0000-000000000007", "AC-CAP-NVY", "Embroidered Cap", 20),
        ("a1000000-0000-0000-0000-000000000008", "AC-PEN-SET", "Ballpoint Pen Set (3 pcs)", 80),
        ("a1000000-0000-0000-0000-000000000009", "AC-UMBRELLA", "Foldable Umbrella", 15),
        ("a1000000-0000-0000-0000-000000000010", "AC-JACKET-VAR", "Varsity Jacket - Limited Edition", 1),
        ("a1000000-0000-0000-0000-000000000011", "AC-MUG-RETIRED", "Ceramic Mug (Retired Design)", 10)
    ];

    public static async Task SeedAsync(InventoryDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.StockItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.StockItems.AddRange(OpeningStock.Select(s => new StockItem
        {
            ProductId = Guid.Parse(s.Id),
            Sku = s.Sku,
            ProductName = s.Name,
            QuantityOnHand = s.Quantity,
            QuantityReserved = 0,
            UpdatedAt = now
        }));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded opening stock for {Count} products", OpeningStock.Length);
    }
}

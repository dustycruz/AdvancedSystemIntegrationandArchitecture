using Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

/// <summary>
/// Demo products inserted on first start. The product ids are fixed because the Inventory
/// service seeds stock for the same ids in its own database (database-per-service, no shared tables).
/// AC-JACKET-VAR has stock 1 in Inventory for the insufficient-stock demo, and AC-MUG-RETIRED
/// is inactive to show that only active products are sold.
/// </summary>
public static class CatalogSeedData
{
    public static IReadOnlyList<Product> Products(DateTimeOffset now) =>
    [
        Create("a1000000-0000-0000-0000-000000000001", "AC-HOODIE-NVY", "Angelicum Hoodie - Navy",
            "Heavyweight cotton-blend hoodie in Angelicum navy with the college crest embroidered in gold.", 1250.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000002", "AC-TSHIRT-WHT", "Classic Campus Tee - White",
            "Soft 100% cotton crew-neck tee with a printed Angelicum wordmark.", 450.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000003", "AC-LANYARD-GLD", "Gold Lanyard and ID Holder",
            "Woven gold lanyard with a clear ID holder and safety breakaway clasp.", 150.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000004", "AC-TUMBLER-BLU", "Insulated Tumbler 500 ml",
            "Double-wall stainless tumbler that keeps drinks cold for 24 hours.", 690.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000005", "AC-NOTEBOOK-A5", "A5 Dotted Notebook",
            "120-page dotted notebook with a debossed cover and ribbon marker.", 220.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000006", "AC-TOTE-CNV", "Canvas Tote Bag",
            "Sturdy natural canvas tote with an inner pocket, sized for a laptop.", 380.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000007", "AC-CAP-NVY", "Embroidered Cap",
            "Six-panel navy cap with an adjustable strap and embroidered crest.", 520.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000008", "AC-PEN-SET", "Ballpoint Pen Set (3 pcs)",
            "Three smooth-writing ballpoint pens in blue, black and gold barrels.", 120.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000009", "AC-UMBRELLA", "Foldable Umbrella",
            "Compact wind-resistant umbrella in navy and gold panels.", 640.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000010", "AC-JACKET-VAR", "Varsity Jacket - Limited Edition",
            "Limited-run varsity jacket with chenille lettering. Only one left in stock.", 2890.00m, true, now),
        Create("a1000000-0000-0000-0000-000000000011", "AC-MUG-RETIRED", "Ceramic Mug (Retired Design)",
            "Previous-season mug design, kept in the catalog but no longer for sale.", 300.00m, false, now)
    ];

    public static async Task SeedAsync(CatalogDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = Products(DateTimeOffset.UtcNow);
        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} catalog products", products.Count);
    }

    private static Product Create(string id, string sku, string name, string description, decimal price, bool isActive, DateTimeOffset now) =>
        new()
        {
            Id = Guid.Parse(id),
            Sku = sku,
            Name = name,
            Description = description,
            Price = price,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
}

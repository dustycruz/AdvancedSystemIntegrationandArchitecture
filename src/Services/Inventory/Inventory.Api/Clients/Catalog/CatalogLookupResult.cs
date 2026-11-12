namespace Inventory.Api.Clients.Catalog;

/// <summary>Result of looking a product up in Catalog: either found, or Catalog answered 404.</summary>
public sealed record CatalogLookupResult(CatalogProduct? Product)
{
    public static CatalogLookupResult NotFound { get; } = new((CatalogProduct?)null);

    public bool IsFound => Product is not null;

    public static CatalogLookupResult Found(CatalogProduct product) => new(product);
}

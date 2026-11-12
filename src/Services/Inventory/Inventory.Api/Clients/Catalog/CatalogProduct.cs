namespace Inventory.Api.Clients.Catalog;

/// <summary>The part of a Catalog product this service needs, independent of the generated client types.</summary>
public sealed record CatalogProduct(Guid Id, string Sku, string Name, decimal Price, bool IsActive);

using Microsoft.AspNetCore.Mvc;
using Storefront.Web.Clients;
using Storefront.Web.Models;
using Storefront.Web.Services;

namespace Storefront.Web.Controllers;

/// <summary>Public catalog: active products from Catalog with availability from Inventory, both through the gateway.</summary>
public sealed class HomeController(
    ICatalogClient catalog,
    IInventoryClient inventory,
    CartService cart,
    ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var products = await catalog.ListProductsAsync(includeInactive: false, cancellationToken);
        var (stock, stockUnavailable) = await TryGetStockAsync(cancellationToken);

        var cards = products
            .Where(p => string.IsNullOrWhiteSpace(q)
                || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ProductCard(p, stock?.GetValueOrDefault(p.Id)))
            .ToList();

        return View(new CatalogViewModel(cards, q, stockUnavailable));
    }

    [HttpGet("/products/{id:guid}")]
    public async Task<IActionResult> Product(Guid id, CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductByIdAsync(id, cancellationToken);
        if (!product.IsActive)
        {
            return NotFound();
        }

        int? available = null;
        try
        {
            available = (await inventory.GetStockByProductIdAsync(id, cancellationToken)).QuantityAvailable;
        }
        catch (GatewayApiException ex)
        {
            logger.LogWarning("Stock for {ProductId} unavailable: {StatusCode}", id, ex.StatusCode);
        }

        return View(new ProductDetailsViewModel(new ProductCard(product, available), cart.GetItems().GetValueOrDefault(id)));
    }

    private async Task<(Dictionary<Guid, int>? Stock, bool Unavailable)> TryGetStockAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stock = await inventory.ListStockAsync(cancellationToken);
            return (stock.ToDictionary(s => s.ProductId, s => s.QuantityAvailable), false);
        }
        catch (GatewayApiException ex)
        {
            // Browsing still works when Inventory is down; availability is just not shown.
            logger.LogWarning("Stock levels unavailable: {StatusCode}", ex.StatusCode);
            return (null, true);
        }
    }
}

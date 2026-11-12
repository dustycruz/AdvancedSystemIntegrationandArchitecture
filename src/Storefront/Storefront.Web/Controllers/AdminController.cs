using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storefront.Web.Clients;
using Storefront.Web.Infrastructure;
using Storefront.Web.Models;

namespace Storefront.Web.Controllers;

/// <summary>
/// Admin pages: products, stock and the Chaos &amp; Saga Testing switches. Everything goes through the gateway,
/// which checks the Admin role again (RouteClaimsRequirement), and the services check it a third time.
/// </summary>
[Authorize(Roles = "Admin")]
public sealed class AdminController(
    ICatalogClient catalog,
    IInventoryClient inventory,
    IOrderClient orders,
    IPaymentClient payments,
    ILogger<AdminController> logger) : Controller
{
    // ---------------------------------------------------------------- products

    public async Task<IActionResult> Products(CancellationToken cancellationToken) =>
        View(await catalog.ListProductsAsync(includeInactive: true, cancellationToken));

    [HttpGet]
    public IActionResult CreateProduct() => View("ProductForm", new ProductFormViewModel());

    [HttpGet]
    public async Task<IActionResult> EditProduct(Guid id, CancellationToken cancellationToken)
    {
        var p = await catalog.GetProductByIdAsync(id, cancellationToken);
        return View("ProductForm", new ProductFormViewModel
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProduct(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("ProductForm", model);
        }

        try
        {
            if (model.IsNew)
            {
                var created = await catalog.CreateProductAsync(new CreateProductRequest
                {
                    Sku = model.Sku.Trim().ToUpperInvariant(),
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive
                }, cancellationToken);
                TempData.Success($"Created {created.Name}. Add opening stock on the Stock page.");
            }
            else
            {
                var updated = await catalog.UpdateProductAsync(new UpdateProductRequest
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive
                }, model.Id!.Value, cancellationToken);
                TempData.Success($"Saved {updated.Name}.");
            }

            return RedirectToAction(nameof(Products));
        }
        catch (GatewayApiException ex) when (ex.StatusCode is 400 or 409)
        {
            AddErrors(ApiErrors.From(ex), ex.StatusCode == 409 ? nameof(model.Sku) : string.Empty);
            return View("ProductForm", model);
        }
    }

    // ---------------------------------------------------------------- stock

    public async Task<IActionResult> Stock(CancellationToken cancellationToken)
    {
        var stock = await inventory.ListStockAsync(cancellationToken);
        var products = (await catalog.ListProductsAsync(includeInactive: true, cancellationToken)).ToDictionary(p => p.Id);
        var rows = stock
            .Select(s => new StockRow(s, products.TryGetValue(s.ProductId, out var p) && p.IsActive))
            .OrderBy(r => r.Stock.ProductName)
            .ToList();
        ViewBag.Unstocked = products.Values.Where(p => stock.All(s => s.ProductId != p.Id)).OrderBy(p => p.Name).ToList();
        return View(new StockViewModel(rows));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(Guid productId, int quantityDelta, string reason, CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventory.AdjustStockAsync(new StockAdjustmentRequest
            {
                QuantityDelta = quantityDelta,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Manual adjustment" : reason
            }, productId, cancellationToken);
            TempData.Success($"{result.ProductName}: {quantityDelta:+#;-#;0} → {result.QuantityOnHand} on hand ({result.QuantityAvailable} available).");
        }
        catch (GatewayApiException ex) when (ex.StatusCode is 400 or 404 or 409 or 503)
        {
            var error = ApiErrors.From(ex);
            TempData.Error($"{error.Title}: {error.Message}");
        }

        return RedirectToAction(nameof(Stock));
    }

    // ---------------------------------------------------------------- chaos & saga testing

    public async Task<IActionResult> Chaos(CancellationToken cancellationToken)
    {
        var paymentMode = (await payments.GetPaymentFailureModeAsync(cancellationToken)).Mode;
        var orderMode = (await orders.GetOrderFailureModeAsync(cancellationToken)).Mode;

        ChaosSettings? chaos = null;
        try
        {
            chaos = await catalog.GetChaosSettingsAsync(cancellationToken);
        }
        catch (GatewayApiException ex) when (ex.StatusCode == 404)
        {
            logger.LogInformation("Catalog chaos settings are not available (not running in Development)");
        }

        var recent = (await orders.ListOrdersAsync(cancellationToken)).Take(8).ToList();
        return View(new ChaosViewModel(paymentMode, orderMode, chaos, recent));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPaymentMode(PaymentFailureMode mode, CancellationToken cancellationToken)
    {
        await payments.SetPaymentFailureModeAsync(new PaymentFailureModeRequest { Mode = mode }, cancellationToken);
        TempData.Success(mode == PaymentFailureMode.None
            ? "Payments work normally again."
            : "Payments will now be declined: the next order releases its stock and is cancelled.");
        return RedirectToAction(nameof(Chaos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetOrderMode(OrderFailureMode mode, CancellationToken cancellationToken)
    {
        await orders.SetOrderFailureModeAsync(new OrderFailureModeRequest { Mode = mode }, cancellationToken);
        TempData.Success(mode == OrderFailureMode.None
            ? "Orders are confirmed normally again."
            : "The next order fails after payment: it is refunded, its stock released and it is cancelled.");
        return RedirectToAction(nameof(Chaos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCatalogChaos(int failurePercent, int delayMs, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await catalog.UpdateChaosSettingsAsync(new ChaosSettings
            {
                FailureRate = Math.Clamp(failurePercent, 0, 100) / 100.0,
                DelayMs = Math.Clamp(delayMs, 0, 30000)
            }, cancellationToken);
            TempData.Success(settings.FailureRate == 0 && settings.DelayMs == 0
                ? "Catalog chaos is off."
                : $"Catalog now fails {settings.FailureRate:P0} of product reads with {settings.DelayMs} ms delay. Order and Inventory retry automatically.");
        }
        catch (GatewayApiException ex) when (ex.StatusCode is 400 or 404)
        {
            TempData.Error(ApiErrors.From(ex).Message);
        }

        return RedirectToAction(nameof(Chaos));
    }

    private void AddErrors(ApiError error, string defaultField)
    {
        foreach (var (field, messages) in error.FieldErrors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(field, message);
            }
        }

        if (error.FieldErrors.Count == 0)
        {
            ModelState.AddModelError(defaultField, error.Message);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storefront.Web.Clients;
using Storefront.Web.Infrastructure;
using Storefront.Web.Models;
using Storefront.Web.Services;

namespace Storefront.Web.Controllers;

/// <summary>Cart, checkout and placing the order (POST /orders/v1/orders through the gateway).</summary>
public sealed class CartController(
    CartService cart,
    ICatalogClient catalog,
    IInventoryClient inventory,
    IOrderClient orders,
    ILogger<CartController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await BuildCartAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, int quantity, string? returnUrl, CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductByIdAsync(productId, cancellationToken);
        if (!product.IsActive)
        {
            TempData.Error($"{product.Name} is no longer for sale.");
        }
        else
        {
            cart.Add(productId, Math.Clamp(quantity, 1, CartService.MaxQuantityPerProduct));
            TempData.Success($"Added {product.Name} to your cart.");
        }

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(Guid productId, int quantity)
    {
        cart.Set(productId, quantity);
        TempData.Info(quantity <= 0 ? "Item removed from your cart." : "Cart updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        if (User.IsAdmin())
        {
            TempData.Info("Administrators cannot place orders. Sign in as a customer to try the checkout.");
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildCartAsync(cancellationToken);
        if (model.Lines.Count == 0)
        {
            TempData.Info("Your cart is empty.");
            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CancellationToken cancellationToken)
    {
        var items = cart.GetItems();
        if (items.Count == 0)
        {
            TempData.Info("Your cart is empty.");
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var order = await orders.PlaceOrderAsync(new PlaceOrderRequest
            {
                Lines = items.Select(i => new PlaceOrderLineRequest { ProductId = i.Key, Quantity = i.Value }).ToList()
            }, cancellationToken);

            cart.Clear();
            TempData.Success("Order received! Watch it move through stock reservation and payment.");
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }
        catch (GatewayApiException ex) when (ex.StatusCode is 400 or 404 or 409 or 503)
        {
            var error = ApiErrors.From(ex);
            logger.LogWarning("Placing an order failed with {StatusCode}: {Message}", ex.StatusCode, error.Message);
            TempData.Error($"{error.Title}: {error.Message}");
            return RedirectToAction(nameof(Checkout));
        }
    }

    private async Task<CartViewModel> BuildCartAsync(CancellationToken cancellationToken)
    {
        var items = cart.GetItems();
        if (items.Count == 0)
        {
            return new CartViewModel([], []);
        }

        Dictionary<Guid, int>? stock = null;
        try
        {
            stock = (await inventory.ListStockAsync(cancellationToken)).ToDictionary(s => s.ProductId, s => s.QuantityAvailable);
        }
        catch (GatewayApiException ex)
        {
            logger.LogWarning("Stock levels unavailable for the cart: {StatusCode}", ex.StatusCode);
        }

        var lines = new List<CartLine>();
        var missing = new List<Guid>();
        foreach (var (productId, quantity) in items)
        {
            try
            {
                var product = await catalog.GetProductByIdAsync(productId, cancellationToken);
                lines.Add(new CartLine(product, quantity, stock?.GetValueOrDefault(productId)));
            }
            catch (GatewayApiException ex) when (ex.StatusCode == 404)
            {
                missing.Add(productId);
                cart.Set(productId, 0);
            }
        }

        return new CartViewModel(lines.OrderBy(l => l.Product.Name).ToList(), missing);
    }
}

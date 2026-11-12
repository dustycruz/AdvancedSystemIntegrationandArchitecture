using Asp.Versioning;
using Inventory.Api.Clients.Catalog;
using Inventory.Api.Contracts;
using Inventory.Api.Data;
using Inventory.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Observability.ProblemDetails;

namespace Inventory.Api.Controllers;

/// <summary>Stock levels, as defined in contracts/inventory.v1.yaml (tag Stock).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stock")]
[Produces("application/json")]
public sealed class StockController(
    InventoryDbContext db,
    ICatalogProductLookup catalog,
    ILogger<StockController> logger) : ControllerBase
{
    private const int MaxConcurrencyRetries = 3;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<StockItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockItemResponse>>> ListStock(CancellationToken cancellationToken)
    {
        var items = await db.StockItems.AsNoTracking().OrderBy(i => i.ProductName).ToListAsync(cancellationToken);
        return Ok(items.Select(StockItemResponse.From).ToList());
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<StockItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<StockItemResponse>> GetStockByProductId(Guid productId, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken)
            ?? throw new NotFoundException($"There is no stock record for product {productId}.");
        return Ok(StockItemResponse.From(item));
    }

    /// <summary>
    /// Applies a manual stock change after validating the product with Catalog over HTTP
    /// (generated client + resilience pipeline). Concurrent reservations are handled with the xmin
    /// concurrency token: a conflicting update is re-read and re-applied.
    /// </summary>
    [HttpPost("{productId:guid}/adjustments")]
    [Authorize(Roles = "Admin")]
    [Consumes("application/json")]
    [ProducesResponseType<StockItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<StockItemResponse>> AdjustStock(
        Guid productId,
        StockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QuantityDelta == 0)
        {
            ModelState.AddModelError(nameof(request.QuantityDelta), "The quantity change cannot be zero.");
            return ValidationProblem(ModelState);
        }

        var lookup = await catalog.GetProductAsync(productId, cancellationToken);
        if (!lookup.IsFound)
        {
            throw new NotFoundException($"Product {productId} does not exist in the catalog.");
        }

        var product = lookup.Product!;
        var adjustedBy = User.Identity?.Name ?? "unknown admin";

        for (var attempt = 1; ; attempt++)
        {
            var item = await db.StockItems.FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            if (item is null)
            {
                if (request.QuantityDelta < 0)
                {
                    throw new ConflictException($"{product.Name} has no stock yet, so stock cannot be removed.");
                }

                item = new StockItem { ProductId = productId, QuantityOnHand = 0, QuantityReserved = 0 };
                db.StockItems.Add(item);
            }

            var newOnHand = item.QuantityOnHand + request.QuantityDelta;
            if (newOnHand < item.QuantityReserved)
            {
                throw new ConflictException(
                    $"Removing {-request.QuantityDelta} would leave {newOnHand} of {product.Name} on hand, " +
                    $"but {item.QuantityReserved} are reserved for orders in progress.");
            }

            item.QuantityOnHand = newOnHand;
            item.Sku = product.Sku;
            item.ProductName = product.Name;
            item.UpdatedAt = now;

            db.Adjustments.Add(new StockAdjustment
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                QuantityDelta = request.QuantityDelta,
                QuantityOnHandAfter = newOnHand,
                Reason = request.Reason.Trim(),
                AdjustedBy = adjustedBy,
                CreatedAt = now
            });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Stock of {Sku} adjusted by {Delta} to {OnHand} by {User}: {Reason}",
                    item.Sku, request.QuantityDelta, item.QuantityOnHand, adjustedBy, request.Reason);
                return Ok(StockItemResponse.From(item));
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                logger.LogInformation("Stock of {ProductId} changed concurrently; re-applying the adjustment (attempt {Attempt})",
                    productId, attempt + 1);
                db.ChangeTracker.Clear();
            }
        }
    }
}

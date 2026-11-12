using Asp.Versioning;
using Catalog.Api.Contracts;
using Catalog.Api.Data;
using Catalog.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Store.Observability.ProblemDetails;

namespace Catalog.Api.Controllers;

/// <summary>Products, as defined in contracts/catalog.v1.yaml (tag Products).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Produces("application/json")]
public sealed class ProductsController(CatalogDbContext db, ILogger<ProductsController> logger) : ControllerBase
{
    private const string AdminRole = "Admin";

    /// <summary>Lists active products ordered by name; includeInactive=true (Admin only) also returns inactive ones.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> ListProducts(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (includeInactive && !User.IsInRole(AdminRole))
        {
            return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();
        }

        var query = db.Products.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return Ok(products.Select(ProductResponse.From).ToList());
    }

    /// <summary>Returns one product, active or not, so callers can tell "unknown" from "not for sale".</summary>
    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> GetProductById(Guid productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException($"Product {productId} does not exist.");
        logger.LogInformation("Served product {ProductId} {Sku} (price {Price}, active {IsActive})",
            product.Id, product.Sku, product.Price, product.IsActive);
        return Ok(ProductResponse.From(product));
    }

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
        {
            throw new ConflictException($"A product with SKU {sku} already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = decimal.Round(request.Price, 2),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Products.Add(product);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException($"A product with SKU {sku} already exists.");
        }

        logger.LogInformation("Created product {ProductId} {Sku} at {Price}", product.Id, product.Sku, product.Price);
        return CreatedAtAction(nameof(GetProductById),
            new { productId = product.Id, version = RouteData.Values["version"] },
            ProductResponse.From(product));
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = AdminRole)]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException($"Product {productId} does not exist.");

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = decimal.Round(request.Price, 2);
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.IsActive = request.IsActive!.Value;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated product {ProductId} {Sku}: price {Price}, active {IsActive}",
            product.Id, product.Sku, product.Price, product.IsActive);
        return Ok(ProductResponse.From(product));
    }
}

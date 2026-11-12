using System.ComponentModel.DataAnnotations;
using Catalog.Api.Domain;

namespace Catalog.Api.Contracts;

/// <summary>ProductResponse in contracts/catalog.v1.yaml.</summary>
public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProductResponse From(Product product) => new(
        product.Id, product.Sku, product.Name, product.Description, product.Price,
        product.ImageUrl, product.IsActive, product.CreatedAt, product.UpdatedAt);
}

/// <summary>CreateProductRequest in contracts/catalog.v1.yaml.</summary>
public sealed record CreateProductRequest
{
    [Required, StringLength(40, MinimumLength = 3), RegularExpression("^[A-Z0-9-]+$",
        ErrorMessage = "SKU may contain only upper-case letters, digits and hyphens.")]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Price { get; init; }

    [StringLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsActive { get; init; } = true;
}

/// <summary>UpdateProductRequest in contracts/catalog.v1.yaml.</summary>
public sealed record UpdateProductRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Price { get; init; }

    [StringLength(500)]
    public string? ImageUrl { get; init; }

    [Required]
    public bool? IsActive { get; init; }
}

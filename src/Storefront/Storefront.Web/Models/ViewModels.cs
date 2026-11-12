using System.ComponentModel.DataAnnotations;
using Storefront.Web.Clients;

namespace Storefront.Web.Models;

public sealed record ProductCard(ProductResponse Product, int? Available)
{
    public bool InStock => Available is null or > 0;
    public bool LowStock => Available is > 0 and <= 5;
}

public sealed record CatalogViewModel(IReadOnlyList<ProductCard> Products, string? Search, bool StockUnavailable);

public sealed record ProductDetailsViewModel(ProductCard Card, int InCart);

public sealed record CartLine(ProductResponse Product, int Quantity, int? Available)
{
    public decimal LineTotal => Product.Price * Quantity;
    public bool Problem => !Product.IsActive || Available is not null && Available < Quantity;
}

public sealed record CartViewModel(IReadOnlyList<CartLine> Lines, IReadOnlyList<Guid> MissingProducts)
{
    public decimal Total => Lines.Sum(l => l.LineTotal);
    public int ItemCount => Lines.Sum(l => l.Quantity);
    public bool CanCheckout => Lines.Count > 0 && Lines.All(l => !l.Problem);
}

public sealed class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match."), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ProductFormViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(40, MinimumLength = 3), RegularExpression("^[A-Z0-9-]+$", ErrorMessage = "Use upper-case letters, digits and hyphens.")]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Enter a price between 0.01 and 1,000,000.")]
    public decimal Price { get; set; }

    [StringLength(500), Url, Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "For sale (active)")]
    public bool IsActive { get; set; } = true;

    public bool IsNew => Id is null;
}

public sealed record StockRow(StockItemResponse Stock, bool ProductActive);

public sealed record StockViewModel(IReadOnlyList<StockRow> Rows);

public sealed record ChaosViewModel(
    PaymentFailureMode PaymentMode,
    OrderFailureMode OrderMode,
    ChaosSettings? CatalogChaos,
    IReadOnlyList<OrderSummaryResponse> RecentOrders);

public sealed record OrdersViewModel(IReadOnlyList<OrderSummaryResponse> Orders, bool AllOrders);

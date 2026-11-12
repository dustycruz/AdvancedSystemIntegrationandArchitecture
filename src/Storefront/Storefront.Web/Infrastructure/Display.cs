using System.Globalization;
using Storefront.Web.Clients;

namespace Storefront.Web.Infrastructure;

/// <summary>Formatting helpers shared by the views.</summary>
public static class Display
{
    public static string Money(decimal amount) => "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Order placed",
        OrderStatus.StockReserved => "Stock reserved",
        OrderStatus.PaymentCharged => "Payment charged",
        OrderStatus.Compensating => "Compensating",
        OrderStatus.Confirmed => "Order confirmed",
        OrderStatus.Cancelled => "Order cancelled",
        _ => status.ToString()
    };

    public static string StatusIcon(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bi-receipt",
        OrderStatus.StockReserved => "bi-box-seam",
        OrderStatus.PaymentCharged => "bi-credit-card",
        OrderStatus.Compensating => "bi-arrow-counterclockwise",
        OrderStatus.Confirmed => "bi-check2-circle",
        OrderStatus.Cancelled => "bi-x-octagon",
        _ => "bi-circle"
    };

    /// <summary>Picks a Bootstrap icon for products without an image, from words in the SKU.</summary>
    public static string ProductIcon(string sku) => sku switch
    {
        _ when sku.Contains("HOODIE") || sku.Contains("JACKET") => "bi-person-standing-dress",
        _ when sku.Contains("TSHIRT") => "bi-person-arms-up",
        _ when sku.Contains("CAP") => "bi-mortarboard",
        _ when sku.Contains("TUMBLER") || sku.Contains("MUG") => "bi-cup-hot",
        _ when sku.Contains("NOTEBOOK") => "bi-journal-bookmark",
        _ when sku.Contains("PEN") => "bi-pen",
        _ when sku.Contains("TOTE") => "bi-bag-heart",
        _ when sku.Contains("UMBRELLA") => "bi-umbrella",
        _ when sku.Contains("LANYARD") => "bi-person-badge",
        _ => "bi-bag"
    };
}

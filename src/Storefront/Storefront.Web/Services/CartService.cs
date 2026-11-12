using System.Text.Json;

namespace Storefront.Web.Services;

/// <summary>
/// The shopping cart lives in an HttpOnly cookie as product id → quantity. Prices are never stored here:
/// they are always read from Catalog, and the order service snapshots them when the order is placed.
/// </summary>
public sealed class CartService(IHttpContextAccessor httpContextAccessor)
{
    public const int MaxQuantityPerProduct = 10;
    private const string CookieName = "store_cart";

    public IReadOnlyDictionary<Guid, int> GetItems()
    {
        var raw = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<Guid, int>();
        }

        try
        {
            var items = JsonSerializer.Deserialize<Dictionary<Guid, int>>(raw) ?? [];
            return items.Where(i => i.Value > 0).ToDictionary(i => i.Key, i => Math.Min(i.Value, MaxQuantityPerProduct));
        }
        catch (JsonException)
        {
            return new Dictionary<Guid, int>();
        }
    }

    public int Count => GetItems().Values.Sum();

    public void Add(Guid productId, int quantity)
    {
        var items = GetItems().ToDictionary();
        items[productId] = Math.Clamp(items.GetValueOrDefault(productId) + quantity, 1, MaxQuantityPerProduct);
        Save(items);
    }

    public void Set(Guid productId, int quantity)
    {
        var items = GetItems().ToDictionary();
        if (quantity <= 0)
        {
            items.Remove(productId);
        }
        else
        {
            items[productId] = Math.Min(quantity, MaxQuantityPerProduct);
        }

        Save(items);
    }

    public void Clear() => Save([]);

    private void Save(Dictionary<Guid, int> items)
    {
        var response = httpContextAccessor.HttpContext!.Response;
        if (items.Count == 0)
        {
            response.Cookies.Delete(CookieName);
            return;
        }

        response.Cookies.Append(CookieName, JsonSerializer.Serialize(items), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}

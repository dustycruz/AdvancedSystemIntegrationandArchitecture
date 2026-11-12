namespace Storefront.Web.Infrastructure;

/// <summary>The JWT from Identity is kept in this HttpOnly cookie; JavaScript can never read it.</summary>
public static class AuthCookie
{
    public const string Name = "store_access_token";

    public static void Write(HttpResponse response, string token, DateTimeOffset expiresAt, bool secure) =>
        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt,
            IsEssential = true,
            Path = "/"
        });

    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, new CookieOptions { Path = "/" });
}

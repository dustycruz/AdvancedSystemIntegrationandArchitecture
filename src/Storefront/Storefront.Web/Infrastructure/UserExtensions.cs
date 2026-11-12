using System.Security.Claims;

namespace Storefront.Web.Infrastructure;

public static class UserExtensions
{
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirst("name")?.Value ?? user.FindFirst("email")?.Value ?? "Account";
}

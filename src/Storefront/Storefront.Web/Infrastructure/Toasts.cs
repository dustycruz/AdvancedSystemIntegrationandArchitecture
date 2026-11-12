using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Storefront.Web.Infrastructure;

/// <summary>One-shot notifications shown as animated toasts on the next page.</summary>
public static class Toasts
{
    private const string Key = "toast";

    public static void Success(this ITempDataDictionary tempData, string message) => Set(tempData, "success", message);
    public static void Error(this ITempDataDictionary tempData, string message) => Set(tempData, "danger", message);
    public static void Info(this ITempDataDictionary tempData, string message) => Set(tempData, "info", message);

    public static (string Kind, string Message)? Take(ITempDataDictionary tempData)
    {
        if (tempData[Key] is not string value)
        {
            return null;
        }

        var separator = value.IndexOf('|');
        return separator < 0 ? ("info", value) : (value[..separator], value[(separator + 1)..]);
    }

    private static void Set(ITempDataDictionary tempData, string kind, string message) => tempData[Key] = $"{kind}|{message}";
}

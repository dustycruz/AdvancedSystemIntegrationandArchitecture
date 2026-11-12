using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Storefront.Web.Clients;

namespace Storefront.Web.Infrastructure;

/// <summary>
/// Last line of defence for gateway errors that an action did not handle itself: 401 signs the user out and
/// sends them to the login page, 403 and 404 show the matching friendly page, anything else the error page
/// with a readable message and the correlation id to quote.
/// </summary>
public sealed class GatewayApiExceptionFilter(ITempDataDictionaryFactory tempDataFactory, ILogger<GatewayApiExceptionFilter> logger)
    : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not GatewayApiException exception)
        {
            return;
        }

        var request = context.HttpContext.Request;
        logger.LogWarning("Gateway call failed with {StatusCode} while serving {Method} {Path}",
            exception.StatusCode, request.Method, request.Path);

        switch (exception.StatusCode)
        {
            case StatusCodes.Status401Unauthorized:
                AuthCookie.Delete(context.HttpContext.Response);
                context.Result = new RedirectResult($"/Account/Login?returnUrl={Uri.EscapeDataString(request.Path + request.QueryString)}");
                break;
            case StatusCodes.Status403Forbidden:
                context.Result = new RedirectResult("/Error/403");
                break;
            case StatusCodes.Status404NotFound:
                context.Result = new RedirectResult("/Error/404");
                break;
            default:
                var error = ApiErrors.From(exception);
                tempDataFactory.GetTempData(context.HttpContext).Error($"{error.Title}: {error.Message}");
                context.Result = new RedirectResult($"/Error/{(exception.StatusCode is >= 400 and < 600 ? exception.StatusCode : 500)}");
                break;
        }

        context.ExceptionHandled = true;
    }
}

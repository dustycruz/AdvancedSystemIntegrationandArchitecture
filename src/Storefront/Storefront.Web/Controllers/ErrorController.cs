using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Store.Observability.Correlation;

namespace Storefront.Web.Controllers;

public sealed record ErrorViewModel(int StatusCode, string Title, string Message, string Icon, string? CorrelationId);

/// <summary>Friendly pages for 401, 403, 404, 429 and server errors (status code pages and the exception handler).</summary>
[Route("Error")]
public sealed class ErrorController(ICorrelationIdAccessor correlation, ILogger<ErrorController> logger) : Controller
{
    [Route("{statusCode:int}")]
    public IActionResult Status(int statusCode)
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (exception is not null)
        {
            logger.LogError(exception, "Unhandled exception for {Path}", HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path);
        }

        var (title, message, icon) = statusCode switch
        {
            401 => ("Please sign in", "You need to sign in to see this page.", "bi-person-lock"),
            403 => ("This area is for administrators", "Your account does not have permission to open this page.", "bi-shield-lock"),
            404 => ("We couldn't find that page", "The page or product you are looking for does not exist or is no longer available.", "bi-compass"),
            429 => ("Too many attempts", "Please wait a minute before trying again.", "bi-hourglass-split"),
            503 => ("The store is busy", "A part of the store is temporarily unavailable. Please try again in a moment.", "bi-cloud-slash"),
            _ => ("Something went wrong", "We could not complete your request. Please try again.", "bi-exclamation-octagon")
        };

        Response.StatusCode = statusCode is >= 400 and < 600 ? statusCode : 500;
        return View("Status", new ErrorViewModel(statusCode, title, message, icon, correlation.CorrelationId));
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Store.Observability.ProblemDetails;

/// <summary>
/// Converts exceptions into RFC 9457 Problem Details. Expected failures (<see cref="StoreException"/>)
/// keep their status code and message; anything else becomes a 500 without internal details.
/// The correlationId extension is added by the Problem Details customisation.
/// </summary>
public sealed class StoreExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<StoreExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            StoreException store => (store.StatusCode, store.Title, store.Message),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request is not valid.", badRequest.Message),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (StatusCodes.Status499ClientClosedRequest, "The client closed the request.", "The request was cancelled by the client."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.",
                "The server could not complete the request. Quote the correlation id when reporting this problem.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request {Method} {Path} failed with {StatusCode}",
                httpContext.Request.Method, httpContext.Request.Path, status);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} returned {StatusCode}: {Detail}",
                httpContext.Request.Method, httpContext.Request.Path, status, detail);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }
}

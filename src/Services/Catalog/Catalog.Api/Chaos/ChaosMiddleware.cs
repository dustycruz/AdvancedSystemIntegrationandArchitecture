using Catalog.Api.Contracts;
using Store.Observability.ProblemDetails;

namespace Catalog.Api.Chaos;

/// <summary>
/// Development-only fault injection for product reads (GET /api/v1/products...). Adds the configured delay
/// and fails the configured share of requests with 503, so callers' retry, timeout and circuit-breaker
/// policies can be demonstrated against a real service.
/// </summary>
public sealed class ChaosMiddleware(RequestDelegate next, ChaosState state, ILogger<ChaosMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsProductRead(context.Request))
        {
            await next(context);
            return;
        }

        ChaosSettings settings = state.Current;
        if (settings.DelayMs > 0)
        {
            logger.LogWarning("Chaos: delaying {Method} {Path} by {DelayMs} ms", context.Request.Method, context.Request.Path, settings.DelayMs);
            await Task.Delay(settings.DelayMs, context.RequestAborted);
        }

        if (settings.FailureRate > 0 && Random.Shared.NextDouble() < settings.FailureRate)
        {
            logger.LogWarning("Chaos: injecting 503 for {Method} {Path} (failure rate {FailureRate:P0})",
                context.Request.Method, context.Request.Path, settings.FailureRate);
            throw new DownstreamServiceException("Catalog",
                $"Injected fault: the chaos switch fails {settings.FailureRate:P0} of product reads.");
        }

        await next(context);
    }

    private static bool IsProductRead(HttpRequest request) =>
        HttpMethods.IsGet(request.Method)
        && request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        && request.Path.Value!.Contains("/products", StringComparison.OrdinalIgnoreCase);
}

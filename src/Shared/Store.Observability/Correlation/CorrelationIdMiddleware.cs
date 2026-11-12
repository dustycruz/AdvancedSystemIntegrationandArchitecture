using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Store.Observability.Logging;

namespace Store.Observability.Correlation;

/// <summary>
/// Reads X-Correlation-ID from the incoming request (the gateway creates it at the edge),
/// creates one when it is missing, stores it for the rest of the request, echoes it on the
/// response, and opens a logging scope so every log line of the request carries it.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ICorrelationIdAccessor accessor,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIdConstants.HeaderName].ToString();
        var correlationId = CorrelationIdGenerator.Sanitize(incoming) ?? CorrelationIdGenerator.NewId();

        accessor.CorrelationId = correlationId;
        context.Items[CorrelationIdConstants.LogPropertyName] = correlationId;
        context.Request.Headers[CorrelationIdConstants.HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginCorrelationScope(correlationId))
        {
            await next(context);
        }
    }
}

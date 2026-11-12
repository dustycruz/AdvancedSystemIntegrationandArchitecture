using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Store.Observability.Correlation;
using Store.Observability.ProblemDetails;

namespace Store.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Registers correlation id propagation, Problem Details with the correlationId extension,
    /// the global exception handler and console logging with scopes.
    /// </summary>
    public static IHostApplicationBuilder AddStoreObservability(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            options.UseUtcTimestamp = true;
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var accessor = context.HttpContext.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                var correlationId = context.HttpContext.Items[CorrelationIdConstants.LogPropertyName] as string
                    ?? accessor.GetOrCreate();

                context.ProblemDetails.Extensions[CorrelationIdConstants.ProblemDetailsExtension] = correlationId;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Title ??= DefaultTitle(context.ProblemDetails.Status);
            };
        });
        builder.Services.AddExceptionHandler<StoreExceptionHandler>();

        return builder;
    }

    /// <summary>
    /// Adds the correlation id middleware first, then the exception handler and status code pages,
    /// so even 401/403/404 responses without a body become Problem Details carrying the correlation id.
    /// </summary>
    public static WebApplication UseStoreObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    private static string DefaultTitle(int? status) => status switch
    {
        StatusCodes.Status400BadRequest => "The request is not valid.",
        StatusCodes.Status401Unauthorized => "Authentication is required.",
        StatusCodes.Status403Forbidden => "You do not have permission to do this.",
        StatusCodes.Status404NotFound => "The resource was not found.",
        StatusCodes.Status405MethodNotAllowed => "The method is not allowed for this resource.",
        StatusCodes.Status409Conflict => "The request conflicts with the current state.",
        StatusCodes.Status429TooManyRequests => "Too many requests.",
        StatusCodes.Status503ServiceUnavailable => "The service is temporarily unavailable.",
        _ => "An error occurred."
    };
}

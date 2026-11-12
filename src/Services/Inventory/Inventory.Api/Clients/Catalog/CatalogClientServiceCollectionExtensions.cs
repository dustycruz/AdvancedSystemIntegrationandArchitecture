using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Inventory.Api.Clients.Catalog.Generated;
using Polly;
using Store.Observability.Correlation;

namespace Inventory.Api.Clients.Catalog;

public static class CatalogClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers the generated Catalog client as a typed client (IHttpClientFactory) with:
    /// X-Correlation-ID propagation, and the standard resilience pipeline configured explicitly —
    /// retry with exponential backoff and jitter, a per-attempt timeout, a total timeout and a circuit breaker.
    /// </summary>
    public static IServiceCollection AddCatalogClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CatalogClientOptions>()
            .Bind(configuration.GetSection(CatalogClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ICatalogClient, CatalogClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<CatalogClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddStandardResilienceHandler()
            .Configure((options, provider) =>
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Inventory.Api.Clients.Catalog.Resilience");

                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromMilliseconds(300);
                options.Retry.OnRetry = args =>
                {
                    logger.LogWarning("Retrying Catalog call (retry {RetryNumber}/{MaxRetries}) in {Delay} after {Outcome}",
                        args.AttemptNumber + 1, 3, args.RetryDelay,
                        args.Outcome.Exception?.Message ?? $"HTTP {(int?)args.Outcome.Result?.StatusCode}");
                    return ValueTask.CompletedTask;
                };

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
                options.CircuitBreaker.OnOpened = args =>
                {
                    logger.LogError("Catalog circuit opened for {BreakDuration}", args.BreakDuration);
                    return ValueTask.CompletedTask;
                };
                options.CircuitBreaker.OnClosed = _ =>
                {
                    logger.LogInformation("Catalog circuit closed; calls resume");
                    return ValueTask.CompletedTask;
                };
            });

        services.AddScoped<ICatalogProductLookup, CatalogProductLookup>();
        return services;
    }
}

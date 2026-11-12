using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Storefront.Web.Clients;
using Store.Observability.Correlation;

namespace Storefront.Web.Infrastructure;

public static class GatewayClientRegistration
{
    /// <summary>
    /// Registers the five generated clients as typed clients pointing at the gateway, each with the gateway
    /// path rewrite, the Bearer token from the auth cookie, X-Correlation-ID propagation and a resilience
    /// pipeline that retries only safe (GET) requests, so an order is never submitted twice.
    /// </summary>
    public static IServiceCollection AddGatewayClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GatewayOptions>()
            .Bind(configuration.GetSection(GatewayOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpContextAccessor();
        services.AddTransient<BearerTokenHandler>();

        services.AddGatewayClient<ICatalogClient, CatalogClient>("catalog");
        services.AddGatewayClient<IIdentityClient, IdentityClient>("identity");
        services.AddGatewayClient<IInventoryClient, InventoryClient>("inventory");
        services.AddGatewayClient<IOrderClient, OrderClient>("orders");
        services.AddGatewayClient<IPaymentClient, PaymentClient>("payments");
        return services;
    }

    private static void AddGatewayClient<TClient, TImplementation>(this IServiceCollection services, string servicePrefix)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<GatewayOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            })
            .AddHttpMessageHandler(() => new GatewayRouteHandler(servicePrefix))
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.DisableForUnsafeHttpMethods();
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });
    }
}

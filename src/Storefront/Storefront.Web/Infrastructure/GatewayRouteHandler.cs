namespace Storefront.Web.Infrastructure;

/// <summary>
/// The generated clients use the service paths from the contracts (<c>/api/v1/products</c>). The gateway
/// exposes the same operations as <c>/&lt;service&gt;/v1/...</c>, so this handler rewrites the path
/// prefix for the client it is attached to, e.g. <c>/api/v1/products</c> → <c>/catalog/v1/products</c>.
/// </summary>
public sealed class GatewayRouteHandler(string servicePrefix) : DelegatingHandler
{
    private const string ContractPrefix = "/api/v1/";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } uri && uri.AbsolutePath.StartsWith(ContractPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(uri)
            {
                Path = $"/{servicePrefix}/v1/{uri.AbsolutePath[ContractPrefix.Length..]}"
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}

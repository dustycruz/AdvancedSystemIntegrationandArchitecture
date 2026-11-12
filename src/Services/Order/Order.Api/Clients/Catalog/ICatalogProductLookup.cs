using Store.Observability.ProblemDetails;

namespace Order.Api.Clients.Catalog;

/// <summary>Adapter over the generated Catalog client.</summary>
public interface ICatalogProductLookup
{
    /// <summary>
    /// Returns <see cref="CatalogLookupResult.NotFound"/> when Catalog answers 404. Throws
    /// <see cref="DownstreamServiceException"/> (mapped to 503) when Catalog stays unreachable or failing
    /// after the resilience pipeline's retries, timeouts or open circuit.
    /// </summary>
    Task<CatalogLookupResult> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}

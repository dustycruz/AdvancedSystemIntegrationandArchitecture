using Inventory.Api.Clients.Catalog.Generated;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Store.Observability.ProblemDetails;

namespace Inventory.Api.Clients.Catalog;

/// <summary>
/// Translates the generated client's behaviour into this service's terms: 404 becomes a NotFound result,
/// and every transport failure (5xx after retries, timeouts, open circuit, connection errors) becomes a
/// <see cref="DownstreamServiceException"/>, which the Problem Details handler turns into 503.
/// </summary>
public sealed class CatalogProductLookup(ICatalogClient client, ILogger<CatalogProductLookup> logger) : ICatalogProductLookup
{
    private const string ServiceName = "Catalog";

    public async Task<CatalogLookupResult> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            var product = await client.GetProductByIdAsync(productId, cancellationToken);
            return CatalogLookupResult.Found(new CatalogProduct(product.Id, product.Sku, product.Name, product.Price, product.IsActive));
        }
        catch (CatalogApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            logger.LogInformation("Catalog has no product {ProductId}", productId);
            return CatalogLookupResult.NotFound;
        }
        catch (CatalogApiException ex)
        {
            logger.LogWarning("Catalog answered {StatusCode} for product {ProductId} after retries", ex.StatusCode, productId);
            throw new DownstreamServiceException(ServiceName,
                $"The Catalog service answered {ex.StatusCode} while looking up product {productId}.", ex);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning("Catalog circuit is open; not calling Catalog for product {ProductId}", productId);
            throw new DownstreamServiceException(ServiceName,
                "The Catalog service is failing repeatedly, so calls are paused for a short time (circuit open).", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogWarning("Catalog did not answer in time for product {ProductId}", productId);
            throw new DownstreamServiceException(ServiceName, "The Catalog service did not answer in time.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Catalog could not be reached for product {ProductId}: {Error}", productId, ex.Message);
            throw new DownstreamServiceException(ServiceName, "The Catalog service could not be reached.", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DownstreamServiceException(ServiceName, "The call to the Catalog service timed out.", ex);
        }
    }
}

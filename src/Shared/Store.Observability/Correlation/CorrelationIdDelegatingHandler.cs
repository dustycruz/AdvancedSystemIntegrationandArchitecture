namespace Store.Observability.Correlation;

/// <summary>
/// Adds the current X-Correlation-ID to every outgoing HttpClient request, so a call from one
/// service to another stays in the same trace. Register it on typed clients with
/// <c>AddHttpMessageHandler&lt;CorrelationIdDelegatingHandler&gt;()</c>.
/// </summary>
public sealed class CorrelationIdDelegatingHandler(ICorrelationIdAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationIdConstants.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdConstants.HeaderName, accessor.GetOrCreate());
        }

        return base.SendAsync(request, cancellationToken);
    }
}

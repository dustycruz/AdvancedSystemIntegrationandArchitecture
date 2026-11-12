using Microsoft.Extensions.Logging;
using Store.Messaging.Consuming;
using Store.Observability.Correlation;
using Store.Observability.Logging;

namespace Store.Messaging.Correlation;

/// <summary>
/// Restores the correlation id of a consumed message: reads the X-Correlation-ID header (falling back to the
/// event's CorrelationId), makes it the current id for the handler — so events it publishes and HTTP calls
/// it makes carry the same id — and opens a logging scope so every log line of the handler shows it.
/// </summary>
public sealed class CorrelationIdConsumeScopeProvider(
    ICorrelationIdAccessor accessor,
    ILogger<CorrelationIdConsumeScopeProvider> logger) : IConsumeScopeProvider
{
    public IDisposable? BeginScope(MessageContext context, IReadOnlyDictionary<string, object?> headers)
    {
        var fromHeader = headers.TryGetValue(CorrelationIdConstants.HeaderName, out var value) ? value as string : null;
        var correlationId = CorrelationIdGenerator.Sanitize(fromHeader)
            ?? CorrelationIdGenerator.Sanitize(context.CorrelationId)
            ?? CorrelationIdGenerator.NewId();

        var previous = accessor.CorrelationId;
        accessor.CorrelationId = correlationId;
        var scope = logger.BeginCorrelationScope(correlationId);

        return new RestoreOnDispose(() =>
        {
            scope?.Dispose();
            accessor.CorrelationId = previous;
        });
    }

    private sealed class RestoreOnDispose(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}

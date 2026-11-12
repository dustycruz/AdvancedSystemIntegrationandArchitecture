using Microsoft.Extensions.Logging;
using Store.Observability.Correlation;

namespace Store.Observability.Logging;

/// <summary>Helpers that open structured logging scopes used across the platform.</summary>
public static class LoggerScopeExtensions
{
    /// <summary>Opens a scope that adds <c>CorrelationId</c> to every log entry written inside it.</summary>
    public static IDisposable? BeginCorrelationScope(this ILogger logger, string correlationId) =>
        logger.BeginScope(new LogScope(
        [
            new(CorrelationIdConstants.LogPropertyName, correlationId)
        ]));

    /// <summary>
    /// Opens a scope for a consumed message: correlation id, message id, message type and the order it belongs to.
    /// </summary>
    public static IDisposable? BeginMessageScope(
        this ILogger logger,
        string correlationId,
        Guid messageId,
        string messageType,
        Guid orderId) =>
        logger.BeginScope(new LogScope(
        [
            new(CorrelationIdConstants.LogPropertyName, correlationId),
            new("MessageId", messageId),
            new("MessageType", messageType),
            new("OrderId", orderId)
        ]));

    /// <summary>Opens a scope with arbitrary named properties.</summary>
    public static IDisposable? BeginPropertyScope(this ILogger logger, params (string Name, object? Value)[] properties) =>
        logger.BeginScope(new LogScope(properties.Select(p => new KeyValuePair<string, object?>(p.Name, p.Value)).ToList()));
}

namespace Store.Messaging.Consuming;

/// <summary>
/// Extension point that runs around the handling of every consumed message, for example to restore
/// tracing information from the message headers. The returned scope is disposed after the handler finishes.
/// </summary>
public interface IConsumeScopeProvider
{
    IDisposable? BeginScope(MessageContext context, IReadOnlyDictionary<string, object?> headers);
}

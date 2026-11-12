namespace Store.Observability.Correlation;

/// <summary>
/// Gives access to the correlation id of the current logical operation: an HTTP request
/// (set by <see cref="CorrelationIdMiddleware"/>) or a consumed message (set by the consumer).
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>The current correlation id, or <c>null</c> outside of a request or message.</summary>
    string? CorrelationId { get; set; }

    /// <summary>Returns the current correlation id, creating and storing a new one when none is set.</summary>
    string GetOrCreate();
}

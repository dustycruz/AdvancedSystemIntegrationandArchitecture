namespace Store.Observability.Correlation;

/// <summary>
/// <see cref="AsyncLocal{T}"/>-backed accessor, so the id flows across awaits within one
/// request or message without being shared between concurrent operations.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? CorrelationId
    {
        get => Current.Value;
        set => Current.Value = value;
    }

    public string GetOrCreate()
    {
        if (string.IsNullOrWhiteSpace(Current.Value))
        {
            Current.Value = CorrelationIdGenerator.NewId();
        }

        return Current.Value;
    }
}

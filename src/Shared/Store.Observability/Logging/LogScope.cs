namespace Store.Observability.Logging;

/// <summary>
/// Logging scope state that structured providers read as key/value pairs and the console formatter prints
/// readably ("CorrelationId:abc, OrderId:..."). A plain dictionary would print as its type name.
/// </summary>
public sealed class LogScope(IReadOnlyList<KeyValuePair<string, object?>> properties)
    : IReadOnlyList<KeyValuePair<string, object?>>
{
    public KeyValuePair<string, object?> this[int index] => properties[index];

    public int Count => properties.Count;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => properties.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => string.Join(", ", properties.Select(p => $"{p.Key}:{p.Value}"));
}

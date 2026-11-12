namespace Store.Observability.Correlation;

public static class CorrelationIdGenerator
{
    /// <summary>Creates a new correlation id (a GUID in "D" format).</summary>
    public static string NewId() => Guid.NewGuid().ToString("D");

    /// <summary>
    /// Returns <paramref name="candidate"/> when it is a usable correlation id
    /// (non-empty, printable ASCII, at most <see cref="CorrelationIdConstants.MaxLength"/> characters);
    /// otherwise returns <c>null</c> so the caller creates a fresh one.
    /// </summary>
    public static string? Sanitize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var trimmed = candidate.Trim();
        if (trimmed.Length > CorrelationIdConstants.MaxLength)
        {
            return null;
        }

        foreach (var c in trimmed)
        {
            if (c < 0x21 || c > 0x7E)
            {
                return null;
            }
        }

        return trimmed;
    }
}

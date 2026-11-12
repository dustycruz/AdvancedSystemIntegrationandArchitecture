namespace Store.Observability.Correlation;

public static class CorrelationIdConstants
{
    /// <summary>HTTP header (and RabbitMQ message header) that carries the correlation id.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Property name used in logging scopes and in Problem Details responses.</summary>
    public const string LogPropertyName = "CorrelationId";

    /// <summary>Extension member added to every Problem Details response.</summary>
    public const string ProblemDetailsExtension = "correlationId";

    /// <summary>Longest correlation id accepted from a caller; longer values are replaced.</summary>
    public const int MaxLength = 128;
}

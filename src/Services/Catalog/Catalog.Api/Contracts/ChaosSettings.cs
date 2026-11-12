using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Contracts;

/// <summary>ChaosSettings in contracts/catalog.v1.yaml.</summary>
public sealed record ChaosSettings
{
    /// <summary>Share of product reads answered with 503, from 0.0 to 1.0.</summary>
    [Range(0.0, 1.0)]
    public double FailureRate { get; init; }

    /// <summary>Delay added before every product read, in milliseconds.</summary>
    [Range(0, 30000)]
    public int DelayMs { get; init; }
}

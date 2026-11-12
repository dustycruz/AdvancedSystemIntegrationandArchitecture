using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Infrastructure;

/// <summary>JWT signing and validation settings (Jwt__Key, Jwt__Issuer, Jwt__Audience, Jwt__ExpiryMinutes).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)] public string Key { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    /// <summary>Lifetime of issued access tokens.</summary>
    [Range(1, 1440)] public int ExpiryMinutes { get; set; } = 60;
}

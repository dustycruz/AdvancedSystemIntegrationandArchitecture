using System.ComponentModel.DataAnnotations;

namespace Order.Api.Infrastructure;

/// <summary>JWT validation settings shared with Identity (Jwt__Key, Jwt__Issuer, Jwt__Audience).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)] public string Key { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
}

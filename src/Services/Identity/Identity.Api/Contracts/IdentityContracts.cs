using System.ComponentModel.DataAnnotations;
using Identity.Api.Domain;

namespace Identity.Api.Contracts;

/// <summary>RegisterRequest in contracts/identity.v1.yaml.</summary>
public sealed record RegisterRequest
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;
}

/// <summary>LoginRequest in contracts/identity.v1.yaml.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Password { get; init; } = string.Empty;
}

/// <summary>UserResponse in contracts/identity.v1.yaml.</summary>
public sealed record UserResponse(Guid Id, string Email, string FullName, UserRole Role, DateTimeOffset CreatedAt)
{
    public static UserResponse From(User user) => new(user.Id, user.Email, user.FullName, user.Role, user.CreatedAt);
}

/// <summary>AuthResponse in contracts/identity.v1.yaml.</summary>
public sealed record AuthResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, UserResponse User);

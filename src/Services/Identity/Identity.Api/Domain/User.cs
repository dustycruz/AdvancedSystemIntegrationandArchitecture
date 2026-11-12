namespace Identity.Api.Domain;

public sealed class User
{
    public Guid Id { get; set; }

    /// <summary>Email as entered, used for display and as the JWT email claim.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Lower-cased, trimmed email used for uniqueness and login lookups.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>ASP.NET Core Identity PasswordHasher output (PBKDF2 with a per-user salt).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}

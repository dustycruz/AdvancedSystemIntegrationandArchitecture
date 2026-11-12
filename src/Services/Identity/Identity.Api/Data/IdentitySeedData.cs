using Identity.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Api.Data;

public static class IdentitySeedData
{
    /// <summary>
    /// Creates the Admin and Customer demo accounts from configuration when they do not exist yet.
    /// Existing accounts are left untouched, so changing .env later does not reset a password.
    /// </summary>
    public static async Task SeedAsync(
        IdentityDbContext db,
        IOptions<SeedAccountOptions> options,
        IPasswordHasher<User> passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var seed = options.Value;
        await EnsureUserAsync(db, passwordHasher, logger, seed.AdminEmail, seed.AdminPassword, seed.AdminFullName, UserRole.Admin, cancellationToken);
        await EnsureUserAsync(db, passwordHasher, logger, seed.CustomerEmail, seed.CustomerPassword, seed.CustomerFullName, UserRole.Customer, cancellationToken);
    }

    private static async Task EnsureUserAsync(
        IdentityDbContext db,
        IPasswordHasher<User> passwordHasher,
        ILogger logger,
        string? email,
        string? password,
        string fullName,
        UserRole role,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No seed {Role} account configured (Seed:{Role}Email / Seed:{Role}Password); skipping", role, role, role);
            return;
        }

        var normalized = User.Normalize(email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken))
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalized,
            FullName = fullName,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Role} account {Email}", role, user.Email);
    }
}

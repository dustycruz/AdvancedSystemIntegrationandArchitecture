using Identity.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Api.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>Applies pending migrations and seeds the demo accounts, retrying while PostgreSQL is starting.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<IdentityDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                await IdentitySeedData.SeedAsync(db,
                    services.GetRequiredService<IOptions<SeedAccountOptions>>(),
                    services.GetRequiredService<IPasswordHasher<User>>(),
                    logger,
                    cancellationToken);
                logger.LogInformation("Identity database is migrated and seeded");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Identity database is not ready (attempt {Attempt}/{MaxAttempts}); retrying in {Delay}",
                    attempt, MaxAttempts, RetryDelay);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}

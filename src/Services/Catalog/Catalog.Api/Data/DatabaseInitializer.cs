using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Applies pending EF Core migrations and seeds demo data, retrying while PostgreSQL is still
    /// starting (the container can report healthy a moment before it accepts connections).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                await CatalogSeedData.SeedAsync(db, logger, cancellationToken);
                logger.LogInformation("Catalog database is migrated and seeded");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Catalog database is not ready (attempt {Attempt}/{MaxAttempts}); retrying in {Delay}",
                    attempt, MaxAttempts, RetryDelay);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}

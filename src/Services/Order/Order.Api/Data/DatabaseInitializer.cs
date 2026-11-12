using Microsoft.EntityFrameworkCore;

namespace Order.Api.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>Applies pending migrations and seeds the settings row, retrying while PostgreSQL is starting.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                await OrderSeedData.SeedAsync(db, logger, cancellationToken);
                logger.LogInformation("Order database is migrated and seeded");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Order database is not ready (attempt {Attempt}/{MaxAttempts}); retrying in {Delay}",
                    attempt, MaxAttempts, RetryDelay);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}

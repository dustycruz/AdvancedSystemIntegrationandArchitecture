using Microsoft.EntityFrameworkCore;
using Order.Api.Domain;

namespace Order.Api.Data;

public static class OrderSeedData
{
    /// <summary>Creates the settings row with failure mode None on first start.</summary>
    public static async Task SeedAsync(OrderDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Settings.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Settings.Add(new OrderSettings
        {
            Id = OrderSettings.SingletonId,
            FailureMode = OrderFailureMode.None,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded order settings with failure mode {FailureMode}", OrderFailureMode.None);
    }
}

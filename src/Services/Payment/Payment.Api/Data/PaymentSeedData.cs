using Microsoft.EntityFrameworkCore;
using Payment.Api.Domain;

namespace Payment.Api.Data;

public static class PaymentSeedData
{
    /// <summary>Creates the settings row with failure mode None on first start.</summary>
    public static async Task SeedAsync(PaymentDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Settings.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Settings.Add(new PaymentSettings
        {
            Id = PaymentSettings.SingletonId,
            FailureMode = PaymentFailureMode.None,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded payment settings with failure mode {FailureMode}", PaymentFailureMode.None);
    }
}

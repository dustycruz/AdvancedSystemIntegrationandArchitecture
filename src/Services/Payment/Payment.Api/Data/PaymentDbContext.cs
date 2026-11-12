using Microsoft.EntityFrameworkCore;
using Payment.Api.Domain;
using Store.Messaging.Idempotency;

namespace Payment.Api.Data;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Payment> Payments => Set<Domain.Payment>();
    public DbSet<PaymentSettings> Settings => Set<PaymentSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Payment>(payment =>
        {
            payment.ToTable("payments", t => t.HasCheckConstraint("ck_payments_amount_positive", "amount > 0"));
            payment.HasKey(p => p.Id);
            payment.Property(p => p.Id).HasColumnName("id");
            payment.Property(p => p.OrderId).HasColumnName("order_id");
            payment.Property(p => p.CustomerId).HasColumnName("customer_id");
            payment.Property(p => p.Amount).HasColumnName("amount").HasPrecision(12, 2);
            payment.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            payment.Property(p => p.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
            payment.Property(p => p.RefundReason).HasColumnName("refund_reason").HasMaxLength(500);
            payment.Property(p => p.CreatedAt).HasColumnName("created_at");
            payment.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            payment.HasIndex(p => p.OrderId).IsUnique();
            payment.HasIndex(p => p.CreatedAt);
        });

        modelBuilder.Entity<PaymentSettings>(settings =>
        {
            settings.ToTable("payment_settings");
            settings.HasKey(s => s.Id);
            settings.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
            settings.Property(s => s.FailureMode).HasColumnName("failure_mode").HasConversion<string>().HasMaxLength(30);
            settings.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.AddProcessedMessages();
    }
}

using Microsoft.EntityFrameworkCore;
using Order.Api.Domain;
using Store.Messaging.Idempotency;

namespace Order.Api.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Order> Orders => Set<Domain.Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderStatusHistory> StatusHistory => Set<OrderStatusHistory>();
    public DbSet<OrderSagaState> SagaStates => Set<OrderSagaState>();
    public DbSet<OrderSettings> Settings => Set<OrderSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Order>(order =>
        {
            order.ToTable("orders", t => t.HasCheckConstraint("ck_orders_total_positive", "total_amount > 0"));
            order.HasKey(o => o.Id);
            order.Property(o => o.Id).HasColumnName("id");
            order.Property(o => o.CustomerId).HasColumnName("customer_id");
            order.Property(o => o.CustomerEmail).HasColumnName("customer_email").HasMaxLength(200).IsRequired();
            order.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
            order.Property(o => o.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
            order.Property(o => o.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128).IsRequired();
            order.Property(o => o.CreatedAt).HasColumnName("created_at");
            order.Property(o => o.UpdatedAt).HasColumnName("updated_at");
            order.Ignore(o => o.IsFinal);
            order.HasIndex(o => new { o.CustomerId, o.CreatedAt });
            order.HasIndex(o => o.CreatedAt);

            order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            order.HasMany(o => o.StatusHistory).WithOne().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.ToTable("order_lines", t => t.HasCheckConstraint("ck_order_lines_quantity_positive", "quantity > 0"));
            line.HasKey(l => l.Id);
            line.Property(l => l.Id).HasColumnName("id");
            line.Property(l => l.OrderId).HasColumnName("order_id");
            line.Property(l => l.ProductId).HasColumnName("product_id");
            line.Property(l => l.Sku).HasColumnName("sku").HasMaxLength(40).IsRequired();
            line.Property(l => l.ProductName).HasColumnName("product_name").HasMaxLength(120).IsRequired();
            line.Property(l => l.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
            line.Property(l => l.Quantity).HasColumnName("quantity");
            line.Ignore(l => l.LineTotal);
        });

        modelBuilder.Entity<OrderStatusHistory>(history =>
        {
            history.ToTable("order_status_history");
            history.HasKey(h => h.Id);
            history.Property(h => h.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            history.Property(h => h.OrderId).HasColumnName("order_id");
            history.Property(h => h.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            history.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(500);
            history.Property(h => h.OccurredAt).HasColumnName("occurred_at");
            history.HasIndex(h => new { h.OrderId, h.Id });
        });

        modelBuilder.Entity<OrderSagaState>(saga =>
        {
            saga.ToTable("order_saga_states");
            saga.HasKey(s => s.OrderId);
            saga.Property(s => s.OrderId).HasColumnName("order_id").ValueGeneratedNever();
            saga.Property(s => s.Step).HasColumnName("step").HasConversion<string>().HasMaxLength(40);
            saga.Property(s => s.StockReserved).HasColumnName("stock_reserved");
            saga.Property(s => s.PaymentCharged).HasColumnName("payment_charged");
            saga.Property(s => s.PaymentId).HasColumnName("payment_id");
            saga.Property(s => s.ChargedAmount).HasColumnName("charged_amount").HasPrecision(12, 2);
            saga.Property(s => s.CompensationReason).HasColumnName("compensation_reason").HasMaxLength(500);
            saga.Property(s => s.StartedAt).HasColumnName("started_at");
            saga.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            saga.Property(s => s.Version).IsRowVersion();
            saga.HasOne<Domain.Order>().WithOne().HasForeignKey<OrderSagaState>(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderSettings>(settings =>
        {
            settings.ToTable("order_settings");
            settings.HasKey(s => s.Id);
            settings.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
            settings.Property(s => s.FailureMode).HasColumnName("failure_mode").HasConversion<string>().HasMaxLength(30);
            settings.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.AddProcessedMessages();
    }
}

using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Store.Messaging.Idempotency;

namespace Inventory.Api.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockReservation> Reservations => Set<StockReservation>();
    public DbSet<StockAdjustment> Adjustments => Set<StockAdjustment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockItem>(item =>
        {
            item.ToTable("stock_items", t =>
            {
                t.HasCheckConstraint("ck_stock_items_on_hand_non_negative", "quantity_on_hand >= 0");
                t.HasCheckConstraint("ck_stock_items_reserved_non_negative", "quantity_reserved >= 0");
                t.HasCheckConstraint("ck_stock_items_reserved_within_on_hand", "quantity_reserved <= quantity_on_hand");
            });
            item.HasKey(i => i.ProductId);
            item.Property(i => i.ProductId).HasColumnName("product_id").ValueGeneratedNever();
            item.Property(i => i.Sku).HasColumnName("sku").HasMaxLength(40).IsRequired();
            item.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(120).IsRequired();
            item.Property(i => i.QuantityOnHand).HasColumnName("quantity_on_hand");
            item.Property(i => i.QuantityReserved).HasColumnName("quantity_reserved");
            item.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            item.Property(i => i.Version).IsRowVersion();
            item.Ignore(i => i.QuantityAvailable);
        });

        modelBuilder.Entity<StockReservation>(reservation =>
        {
            reservation.ToTable("stock_reservations", t =>
                t.HasCheckConstraint("ck_stock_reservations_quantity_positive", "quantity > 0"));
            reservation.HasKey(r => r.Id);
            reservation.Property(r => r.Id).HasColumnName("id");
            reservation.Property(r => r.OrderId).HasColumnName("order_id");
            reservation.Property(r => r.ProductId).HasColumnName("product_id");
            reservation.Property(r => r.Quantity).HasColumnName("quantity");
            reservation.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            reservation.Property(r => r.CreatedAt).HasColumnName("created_at");
            reservation.Property(r => r.UpdatedAt).HasColumnName("updated_at");
            reservation.HasIndex(r => r.OrderId);
            reservation.HasIndex(r => new { r.OrderId, r.ProductId }).IsUnique();
            reservation.HasOne<StockItem>().WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockAdjustment>(adjustment =>
        {
            adjustment.ToTable("stock_adjustments");
            adjustment.HasKey(a => a.Id);
            adjustment.Property(a => a.Id).HasColumnName("id");
            adjustment.Property(a => a.ProductId).HasColumnName("product_id");
            adjustment.Property(a => a.QuantityDelta).HasColumnName("quantity_delta");
            adjustment.Property(a => a.QuantityOnHandAfter).HasColumnName("quantity_on_hand_after");
            adjustment.Property(a => a.Reason).HasColumnName("reason").HasMaxLength(200).IsRequired();
            adjustment.Property(a => a.AdjustedBy).HasColumnName("adjusted_by").HasMaxLength(200).IsRequired();
            adjustment.Property(a => a.CreatedAt).HasColumnName("created_at");
            adjustment.HasIndex(a => a.ProductId);
        });

        modelBuilder.AddProcessedMessages();
    }
}

using Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("products");
            product.HasKey(p => p.Id);
            product.Property(p => p.Id).HasColumnName("id");
            product.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(40).IsRequired();
            product.Property(p => p.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            product.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
            product.Property(p => p.Price).HasColumnName("price").HasPrecision(12, 2);
            product.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            product.Property(p => p.IsActive).HasColumnName("is_active");
            product.Property(p => p.CreatedAt).HasColumnName("created_at");
            product.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            product.HasIndex(p => p.Sku).IsUnique();
            product.HasIndex(p => new { p.IsActive, p.Name });
            product.ToTable(t => t.HasCheckConstraint("ck_products_price_positive", "price > 0"));
        });
    }
}

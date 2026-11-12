using Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Id).HasColumnName("id");
            user.Property(u => u.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            user.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(200).IsRequired();
            user.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(120).IsRequired();
            user.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
            user.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
            user.Property(u => u.CreatedAt).HasColumnName("created_at");
            user.HasIndex(u => u.NormalizedEmail).IsUnique();
        });
    }
}

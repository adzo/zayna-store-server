using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Data;

public class StoreDbContext: IdentityDbContext<ApplicationUser>
{
    public StoreDbContext(DbContextOptions<StoreDbContext> options): base(options)
    {

    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ApplicationUser configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => o.OrderNumber).IsUnique();
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(oi => oi.TotalPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
            entity.Property(rt => rt.UserId).IsRequired().HasMaxLength(450);

            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasIndex(rt => rt.UserId);
            entity.HasIndex(rt => rt.ExpiryDate);
            entity.HasIndex(rt => new { rt.UserId, rt.IsRevoked });
        });

        // ProductImage configuration
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.ImageUrl).IsRequired().HasMaxLength(500);

            entity.HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pi => pi.ProductId);
            entity.HasIndex(pi => pi.IsMain);
        });
    }
}
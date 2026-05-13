using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Models;

namespace OrderProcessingApi.Data;

public sealed class OrderProcessingDbContext(DbContextOptions<OrderProcessingDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.HasIndex(product => product.Sku).IsUnique();
            entity.Property(product => product.Name).HasMaxLength(200);
            entity.Property(product => product.Sku).HasMaxLength(64);
            entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.CustomerName).HasMaxLength(200);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(order => order.TotalAmount).HasColumnType("decimal(18,2)");

            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductName).HasMaxLength(200);
            entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}

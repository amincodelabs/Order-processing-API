using InventoryGrpcService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryGrpcService.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<ProductInventory> ProductInventories => Set<ProductInventory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductInventory>(entity =>
        {
            entity.HasKey(inventory => inventory.Id);
            entity.HasIndex(inventory => inventory.ProductId).IsUnique();
            entity.Property(inventory => inventory.ProductId).HasMaxLength(64);
        });
    }
}

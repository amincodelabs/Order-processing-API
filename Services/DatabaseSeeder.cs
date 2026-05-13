using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Data;
using OrderProcessingApi.Models;

namespace OrderProcessingApi.Services;

public sealed class DatabaseSeeder(OrderProcessingDbContext dbContext)
{
    public async Task SeedAsync()
    {
        await dbContext.Database.MigrateAsync();

        if (await dbContext.Products.AnyAsync())
        {
            return;
        }

        dbContext.Products.AddRange(
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Mechanical Keyboard",
                Sku = "KEY-001",
                Price = 129.99m,
                StockQuantity = 25,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Mouse",
                Sku = "MOU-001",
                Price = 49.99m,
                StockQuantity = 40,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "USB-C Dock",
                Sku = "DOC-001",
                Price = 89.50m,
                StockQuantity = 15,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }
}

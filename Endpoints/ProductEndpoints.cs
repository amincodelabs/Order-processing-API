using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Contracts;
using OrderProcessingApi.Data;
using OrderProcessingApi.Models;
using StackExchange.Redis;

namespace OrderProcessingApi.Endpoints;

public static class ProductEndpoints
{
    public const string ProductsCacheKey = "products:list";

    public static RouteGroupBuilder MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/products").WithTags("Products");

        group.MapGet("/", async (OrderProcessingDbContext dbContext, IConnectionMultiplexer redis) =>
        {
            var cache = redis.GetDatabase();
            var cachedProducts = await cache.StringGetAsync(ProductsCacheKey);

            if (cachedProducts.HasValue)
            {
                var productsFromCache = JsonSerializer.Deserialize<List<Product>>(cachedProducts!);
                return Results.Ok(productsFromCache);
            }

            var products = await dbContext.Products
                .AsNoTracking()
                .OrderBy(product => product.Name)
                .ToListAsync();

            await cache.StringSetAsync(
                ProductsCacheKey,
                JsonSerializer.Serialize(products),
                TimeSpan.FromMinutes(5));

            return Results.Ok(products);
        });

        group.MapGet("/{id:guid}", async (Guid id, OrderProcessingDbContext dbContext) =>
        {
            var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        group.MapPost("/", async (
            CreateProductRequest request,
            OrderProcessingDbContext dbContext,
            IConnectionMultiplexer redis) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku))
            {
                return Results.BadRequest("Product name and SKU are required.");
            }

            if (request.Price <= 0 || request.StockQuantity < 0)
            {
                return Results.BadRequest("Price must be positive and stock cannot be negative.");
            }

            var skuExists = await dbContext.Products.AnyAsync(product => product.Sku == request.Sku);
            if (skuExists)
            {
                return Results.Conflict($"A product with SKU '{request.Sku}' already exists.");
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Sku = request.Sku.Trim(),
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();
            await redis.GetDatabase().KeyDeleteAsync(ProductsCacheKey);

            return Results.Created($"/products/{product.Id}", product);
        });

        return group;
    }
}

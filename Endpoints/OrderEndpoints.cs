using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Contracts;
using OrderProcessingApi.Data;
using OrderProcessingApi.Models;
using OrderProcessingApi.Services.Inventory;
using RedisConnectionMultiplexer = StackExchange.Redis.IConnectionMultiplexer;

namespace OrderProcessingApi.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapGet("/", async (OrderProcessingDbContext dbContext) =>
        {
            var orders = await dbContext.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();

            return Results.Ok(orders);
        });

        group.MapGet("/{id:guid}", async (Guid id, OrderProcessingDbContext dbContext) =>
        {
            var order = await dbContext.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .FirstOrDefaultAsync(order => order.Id == id);

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapPost("/", async (
            CreateOrderRequest request,
            OrderProcessingDbContext dbContext,
            RedisConnectionMultiplexer redis,
            IInventoryClient inventoryClient,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return Results.BadRequest("Customer name is required.");
            }

            if (request.Items.Count == 0)
            {
                return Results.BadRequest("At least one order item is required.");
            }

            if (request.Items.Any(item => item.Quantity <= 0))
            {
                return Results.BadRequest("Order item quantity must be greater than zero.");
            }

            var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
            var products = await dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id);

            if (products.Count != productIds.Count)
            {
                return Results.BadRequest("One or more products do not exist.");
            }

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                var reservation = await inventoryClient.ReserveStockAsync(
                    product.Id,
                    item.Quantity,
                    cancellationToken);

                if (!reservation.IsReserved)
                {
                    return Results.BadRequest($"Product '{product.Name}': {reservation.Message}");
                }
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.CustomerName.Trim(),
                Status = OrderStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            foreach (var requestItem in request.Items)
            {
                var product = products[requestItem.ProductId];
                product.StockQuantity = Math.Max(product.StockQuantity - requestItem.Quantity, 0);

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = requestItem.Quantity,
                    UnitPrice = product.Price
                });
            }

            order.TotalAmount = order.Items.Sum(item => item.UnitPrice * item.Quantity);

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
            await redis.GetDatabase().KeyDeleteAsync(ProductEndpoints.ProductsCacheKey);

            return Results.Created($"/orders/{order.Id}", order);
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, OrderStatus status, OrderProcessingDbContext dbContext) =>
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == id);
            if (order is null)
            {
                return Results.NotFound();
            }

            order.Status = status;
            await dbContext.SaveChangesAsync();

            return Results.Ok(order);
        });

        return group;
    }
}

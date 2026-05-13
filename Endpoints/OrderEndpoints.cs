using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Contracts;
using OrderProcessingApi.Data;
using OrderProcessingApi.Models;
using OrderProcessingApi.Services.Orders;

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
            IOrderService orderService,
            CancellationToken cancellationToken) =>
        {
            var result = await orderService.CreateOrderAsync(request, cancellationToken);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(result.ErrorMessage);
            }

            return Results.Created($"/orders/{result.Order!.Id}", result.Order);
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

using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Contracts;
using OrderProcessingApi.Data;
using OrderProcessingApi.Models;
using OrderProcessingApi.Services.Caching;
using OrderProcessingApi.Services.Inventory;

namespace OrderProcessingApi.Services.Orders;

public sealed class OrderService(
    OrderProcessingDbContext dbContext,
    IProductCacheInvalidator productCacheInvalidator,
    IInventoryClient inventoryClient) : IOrderService
{
    public async Task<CreateOrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return CreateOrderResult.Failure("Customer name is required.");
        }

        if (request.Items.Count == 0)
        {
            return CreateOrderResult.Failure("At least one order item is required.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return CreateOrderResult.Failure("Order item quantity must be greater than zero.");
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return CreateOrderResult.Failure("One or more products do not exist.");
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
                return CreateOrderResult.Failure($"Product '{product.Name}': {reservation.Message}");
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
        await dbContext.SaveChangesAsync(cancellationToken);
        await productCacheInvalidator.InvalidateProductsAsync(cancellationToken);

        return CreateOrderResult.Success(order);
    }
}

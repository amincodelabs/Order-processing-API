using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Contracts;
using OrderProcessingApi.Data;
using OrderProcessingApi.Models;
using OrderProcessingApi.Services.Caching;
using OrderProcessingApi.Services.Inventory;
using OrderProcessingApi.Services.Orders;

namespace OrderProcessingApi.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_creates_order_and_reduces_product_stock()
    {
        await using var fixture = await OrderDbFixture.CreateAsync();
        var product = await fixture.AddProductAsync(stockQuantity: 5, price: 10m);
        var inventoryClient = new StubInventoryClient(new InventoryReservationResult(true, 4, "Stock reserved."));
        var cacheInvalidator = new RecordingProductCacheInvalidator();
        var service = CreateService(fixture.DbContext, cacheInvalidator, inventoryClient);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            " Amin ",
            [new CreateOrderItemRequest(product.Id, 2)]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Order);
        Assert.Equal("Amin", result.Order.CustomerName);
        Assert.Equal(20m, result.Order.TotalAmount);
        Assert.Equal(OrderStatus.Pending, result.Order.Status);
        Assert.Single(result.Order.Items);
        Assert.True(cacheInvalidator.WasCalled);

        var updatedProduct = await fixture.DbContext.Products.SingleAsync(item => item.Id == product.Id);
        Assert.Equal(3, updatedProduct.StockQuantity);
    }

    [Fact]
    public async Task CreateOrderAsync_rejects_missing_customer_name()
    {
        await using var fixture = await OrderDbFixture.CreateAsync();
        var service = CreateService(fixture.DbContext);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            "",
            [new CreateOrderItemRequest(Guid.NewGuid(), 1)]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Customer name is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrderAsync_rejects_unknown_product()
    {
        await using var fixture = await OrderDbFixture.CreateAsync();
        var service = CreateService(fixture.DbContext);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            "Amin",
            [new CreateOrderItemRequest(Guid.NewGuid(), 1)]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("One or more products do not exist.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrderAsync_rejects_inventory_reservation_failure()
    {
        await using var fixture = await OrderDbFixture.CreateAsync();
        var product = await fixture.AddProductAsync(stockQuantity: 5, price: 10m);
        var inventoryClient = new StubInventoryClient(new InventoryReservationResult(false, 0, "Insufficient stock."));
        var service = CreateService(fixture.DbContext, inventoryClient: inventoryClient);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            "Amin",
            [new CreateOrderItemRequest(product.Id, 2)]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Product 'Test Product': Insufficient stock.", result.ErrorMessage);
    }

    private static OrderService CreateService(
        OrderProcessingDbContext dbContext,
        IProductCacheInvalidator? cacheInvalidator = null,
        IInventoryClient? inventoryClient = null)
    {
        return new OrderService(
            dbContext,
            cacheInvalidator ?? new RecordingProductCacheInvalidator(),
            inventoryClient ?? new StubInventoryClient(new InventoryReservationResult(true, 0, "Stock reserved.")));
    }

    private sealed class StubInventoryClient(InventoryReservationResult result) : IInventoryClient
    {
        public Task<InventoryReservationResult> ReserveStockAsync(
            Guid productId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingProductCacheInvalidator : IProductCacheInvalidator
    {
        public bool WasCalled { get; private set; }

        public Task InvalidateProductsAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class OrderDbFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private OrderDbFixture(SqliteConnection connection, OrderProcessingDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public OrderProcessingDbContext DbContext { get; }

        public static async Task<OrderDbFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<OrderProcessingDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new OrderProcessingDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new OrderDbFixture(connection, dbContext);
        }

        public async Task<Product> AddProductAsync(int stockQuantity, decimal price)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Sku = $"SKU-{Guid.NewGuid():N}",
                Price = price,
                StockQuantity = stockQuantity,
                CreatedAt = DateTimeOffset.UtcNow
            };

            DbContext.Products.Add(product);
            await DbContext.SaveChangesAsync();

            return product;
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

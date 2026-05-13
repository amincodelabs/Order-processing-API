using Grpc.Core;
using Grpc.Core.Testing;
using InventoryGrpcService.Data;
using InventoryGrpcService.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryGrpcService.Tests;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task CheckStock_creates_default_inventory_for_unknown_product()
    {
        await using var fixture = await InventoryDbFixture.CreateAsync();
        var service = CreateService(fixture.DbContext);

        var reply = await service.CheckStock(new CheckStockRequest
        {
            ProductId = "product-1",
            Quantity = 5
        }, CreateServerCallContext());

        Assert.True(reply.IsAvailable);
        Assert.Equal(100, reply.AvailableQuantity);
        Assert.Equal("Stock is available.", reply.Message);
    }

    [Fact]
    public async Task ReserveStock_reduces_available_quantity()
    {
        await using var fixture = await InventoryDbFixture.CreateAsync();
        var service = CreateService(fixture.DbContext);

        var reply = await service.ReserveStock(new ReserveStockRequest
        {
            ProductId = "product-1",
            Quantity = 7
        }, CreateServerCallContext());

        Assert.True(reply.IsReserved);
        Assert.Equal(93, reply.RemainingQuantity);

        var inventory = await fixture.DbContext.ProductInventories.SingleAsync();
        Assert.Equal("product-1", inventory.ProductId);
        Assert.Equal(93, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task ReserveStock_rejects_request_when_stock_is_insufficient()
    {
        await using var fixture = await InventoryDbFixture.CreateAsync();
        var service = CreateService(fixture.DbContext);

        var reply = await service.ReserveStock(new ReserveStockRequest
        {
            ProductId = "product-1",
            Quantity = 101
        }, CreateServerCallContext());

        Assert.False(reply.IsReserved);
        Assert.Equal(100, reply.RemainingQuantity);
        Assert.Equal("Insufficient stock.", reply.Message);
    }

    [Fact]
    public async Task ReserveStock_rejects_invalid_quantity()
    {
        await using var fixture = await InventoryDbFixture.CreateAsync();
        var service = CreateService(fixture.DbContext);

        var reply = await service.ReserveStock(new ReserveStockRequest
        {
            ProductId = "product-1",
            Quantity = 0
        }, CreateServerCallContext());

        Assert.False(reply.IsReserved);
        Assert.Equal("Quantity must be greater than zero.", reply.Message);
    }

    private static InventoryService CreateService(InventoryDbContext dbContext)
    {
        return new InventoryService(dbContext, NullLogger<InventoryService>.Instance);
    }

    private static ServerCallContext CreateServerCallContext()
    {
        return TestServerCallContext.Create(
            method: "ReserveStock",
            host: "localhost",
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: [],
            cancellationToken: CancellationToken.None,
            peer: "ipv4:127.0.0.1:5000",
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: _ => Task.CompletedTask,
            writeOptionsGetter: () => null,
            writeOptionsSetter: _ => { });
    }

    private sealed class InventoryDbFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private InventoryDbFixture(SqliteConnection connection, InventoryDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public InventoryDbContext DbContext { get; }

        public static async Task<InventoryDbFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new InventoryDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new InventoryDbFixture(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

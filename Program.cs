using Microsoft.EntityFrameworkCore;
using OrderProcessingApi.Data;
using OrderProcessingApi.Endpoints;
using OrderProcessingApi.Services;
using OrderProcessingApi.Services.Caching;
using OrderProcessingApi.Services.Inventory;
using OrderProcessingApi.Services.Orders;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddSingleton<IInventoryClient, GrpcInventoryClient>();
builder.Services.AddScoped<IProductCacheInvalidator, RedisProductCacheInvalidator>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.SeedDatabaseAsync();

app.MapGet("/", () => Results.Ok(new
{
    Name = "Order Processing API",
    Version = "1.0"
}));

app.MapGet("/health", async (OrderProcessingDbContext dbContext, IConnectionMultiplexer redis) =>
{
    var databaseAvailable = await dbContext.Database.CanConnectAsync();
    var redisAvailable = await redis.GetDatabase().PingAsync();

    return Results.Ok(new
    {
        Status = "Healthy",
        Database = databaseAvailable ? "Available" : "Unavailable",
        Redis = $"{redisAvailable.TotalMilliseconds:N0} ms"
    });
});

app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();

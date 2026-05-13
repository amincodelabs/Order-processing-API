using InventoryGrpcService.Data;
using InventoryGrpcService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("InventoryConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapGrpcService<InventoryService>();
app.MapGet("/", () => Results.Ok(new
{
    Name = "Inventory gRPC Service",
    Protocol = "gRPC"
}));

app.Run();

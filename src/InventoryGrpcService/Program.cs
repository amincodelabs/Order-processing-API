using InventoryGrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<InventoryService>();
app.MapGet("/", () => Results.Ok(new
{
    Name = "Inventory gRPC Service",
    Protocol = "gRPC"
}));

app.Run();

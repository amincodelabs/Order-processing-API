using Grpc.Core;
using Grpc.Net.Client;

namespace OrderProcessingApi.Services.Inventory;

public sealed class GrpcInventoryClient : IInventoryClient
{
    private readonly global::InventoryGrpcService.Inventory.InventoryClient _client;
    private readonly ILogger<GrpcInventoryClient> _logger;

    public GrpcInventoryClient(IConfiguration configuration, ILogger<GrpcInventoryClient> logger)
    {
        _logger = logger;

        var address = configuration["InventoryGrpc:Address"]
            ?? throw new InvalidOperationException("InventoryGrpc:Address is not configured.");

        var channel = GrpcChannel.ForAddress(address);
        _client = new global::InventoryGrpcService.Inventory.InventoryClient(channel);
    }

    public async Task<InventoryReservationResult> ReserveStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _client.ReserveStockAsync(new global::InventoryGrpcService.ReserveStockRequest
            {
                ProductId = productId.ToString(),
                Quantity = quantity
            }, cancellationToken: cancellationToken);

            return new InventoryReservationResult(
                reply.IsReserved,
                reply.RemainingQuantity,
                reply.Message);
        }
        catch (RpcException exception)
        {
            _logger.LogError(
                exception,
                "Inventory reservation RPC failed for product {ProductId}.",
                productId);

            return new InventoryReservationResult(
                false,
                0,
                "Inventory service is unavailable.");
        }
    }
}

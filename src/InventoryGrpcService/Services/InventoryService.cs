using System.Collections.Concurrent;
using Grpc.Core;

namespace InventoryGrpcService.Services;

public sealed class InventoryService : Inventory.InventoryBase
{
    private static readonly ConcurrentDictionary<string, int> StockByProductId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public override Task<CheckStockReply> CheckStock(CheckStockRequest request, ServerCallContext context)
    {
        if (!IsValidRequest(request.ProductId, request.Quantity, out var validationMessage))
        {
            return Task.FromResult(new CheckStockReply
            {
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = validationMessage
            });
        }

        var availableQuantity = StockByProductId.GetOrAdd(request.ProductId, _ => 100);
        var isAvailable = availableQuantity >= request.Quantity;

        return Task.FromResult(new CheckStockReply
        {
            IsAvailable = isAvailable,
            AvailableQuantity = availableQuantity,
            Message = isAvailable ? "Stock is available." : "Insufficient stock."
        });
    }

    public override Task<ReserveStockReply> ReserveStock(ReserveStockRequest request, ServerCallContext context)
    {
        if (!IsValidRequest(request.ProductId, request.Quantity, out var validationMessage))
        {
            return Task.FromResult(new ReserveStockReply
            {
                IsReserved = false,
                RemainingQuantity = 0,
                Message = validationMessage
            });
        }

        while (true)
        {
            var currentQuantity = StockByProductId.GetOrAdd(request.ProductId, _ => 100);
            if (currentQuantity < request.Quantity)
            {
                _logger.LogWarning(
                    "Stock reservation failed for product {ProductId}. Requested quantity: {Quantity}",
                    request.ProductId,
                    request.Quantity);

                return Task.FromResult(new ReserveStockReply
                {
                    IsReserved = false,
                    RemainingQuantity = currentQuantity,
                    Message = "Insufficient stock."
                });
            }

            var remainingQuantity = currentQuantity - request.Quantity;
            if (!StockByProductId.TryUpdate(request.ProductId, remainingQuantity, currentQuantity))
            {
                continue;
            }

            return Task.FromResult(new ReserveStockReply
            {
                IsReserved = true,
                RemainingQuantity = remainingQuantity,
                Message = "Stock reserved."
            });
        }
    }

    private static bool IsValidRequest(string productId, int quantity, out string message)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            message = "Product id is required.";
            return false;
        }

        if (quantity <= 0)
        {
            message = "Quantity must be greater than zero.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}

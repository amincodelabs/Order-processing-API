using System.Data;
using Grpc.Core;
using InventoryGrpcService.Data;
using InventoryGrpcService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryGrpcService.Services;

public sealed class InventoryService : Inventory.InventoryBase
{
    private const int DefaultStockQuantity = 100;
    private readonly InventoryDbContext _dbContext;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(InventoryDbContext dbContext, ILogger<InventoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task<CheckStockReply> CheckStock(CheckStockRequest request, ServerCallContext context)
    {
        if (!IsValidRequest(request.ProductId, request.Quantity, out var validationMessage))
        {
            return new CheckStockReply
            {
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = validationMessage
            };
        }

        var inventory = await GetOrCreateInventoryAsync(request.ProductId, context.CancellationToken);
        var isAvailable = inventory.AvailableQuantity >= request.Quantity;

        return new CheckStockReply
        {
            IsAvailable = isAvailable,
            AvailableQuantity = inventory.AvailableQuantity,
            Message = isAvailable ? "Stock is available." : "Insufficient stock."
        };
    }

    public override async Task<ReserveStockReply> ReserveStock(ReserveStockRequest request, ServerCallContext context)
    {
        if (!IsValidRequest(request.ProductId, request.Quantity, out var validationMessage))
        {
            return new ReserveStockReply
            {
                IsReserved = false,
                RemainingQuantity = 0,
                Message = validationMessage
            };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            context.CancellationToken);

        var inventory = await GetOrCreateInventoryAsync(request.ProductId, context.CancellationToken);
        if (inventory.AvailableQuantity < request.Quantity)
        {
            _logger.LogWarning(
                "Stock reservation failed for product {ProductId}. Requested quantity: {Quantity}",
                request.ProductId,
                request.Quantity);

            return new ReserveStockReply
            {
                IsReserved = false,
                RemainingQuantity = inventory.AvailableQuantity,
                Message = "Insufficient stock."
            };
        }

        inventory.AvailableQuantity -= request.Quantity;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);

        return new ReserveStockReply
        {
            IsReserved = true,
            RemainingQuantity = inventory.AvailableQuantity,
            Message = "Stock reserved."
        };
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

    private async Task<ProductInventory> GetOrCreateInventoryAsync(string productId, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.ProductInventories
            .FirstOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        if (inventory is not null)
        {
            return inventory;
        }

        inventory = new ProductInventory
        {
            ProductId = productId,
            AvailableQuantity = DefaultStockQuantity,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ProductInventories.Add(inventory);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return inventory;
    }
}

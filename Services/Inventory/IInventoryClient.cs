namespace OrderProcessingApi.Services.Inventory;

public interface IInventoryClient
{
    Task<InventoryReservationResult> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken);
}

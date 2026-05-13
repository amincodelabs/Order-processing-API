namespace OrderProcessingApi.Services.Inventory;

public sealed record InventoryReservationResult(
    bool IsReserved,
    int RemainingQuantity,
    string Message);

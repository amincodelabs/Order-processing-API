namespace InventoryGrpcService.Models;

public sealed class ProductInventory
{
    public Guid Id { get; set; }
    public required string ProductId { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

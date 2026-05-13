namespace OrderProcessingApi.Models;

public sealed class Order
{
    public Guid Id { get; set; }
    public required string CustomerName { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

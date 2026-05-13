namespace OrderProcessingApi.Models;

using System.Text.Json.Serialization;

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }
}

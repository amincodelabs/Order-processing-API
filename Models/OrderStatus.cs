namespace OrderProcessingApi.Models;

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Packed = 3,
    Shipped = 4,
    Cancelled = 5
}

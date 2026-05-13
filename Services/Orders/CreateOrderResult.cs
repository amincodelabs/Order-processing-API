using OrderProcessingApi.Models;

namespace OrderProcessingApi.Services.Orders;

public sealed record CreateOrderResult(
    bool IsSuccess,
    Order? Order,
    string? ErrorMessage)
{
    public static CreateOrderResult Success(Order order)
    {
        return new CreateOrderResult(true, order, null);
    }

    public static CreateOrderResult Failure(string errorMessage)
    {
        return new CreateOrderResult(false, null, errorMessage);
    }
}

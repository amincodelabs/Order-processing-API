using OrderProcessingApi.Contracts;

namespace OrderProcessingApi.Services.Orders;

public interface IOrderService
{
    Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
}

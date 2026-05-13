namespace OrderProcessingApi.Contracts;

public sealed record CreateOrderRequest(
    string CustomerName,
    List<CreateOrderItemRequest> Items);

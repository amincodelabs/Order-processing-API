namespace OrderProcessingApi.Contracts;

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity);

namespace OrderProcessingApi.Contracts;

public sealed record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity);

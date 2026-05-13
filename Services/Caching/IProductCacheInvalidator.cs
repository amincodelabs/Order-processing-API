namespace OrderProcessingApi.Services.Caching;

public interface IProductCacheInvalidator
{
    Task InvalidateProductsAsync(CancellationToken cancellationToken);
}

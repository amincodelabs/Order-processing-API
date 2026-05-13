using OrderProcessingApi.Endpoints;
using RedisConnectionMultiplexer = StackExchange.Redis.IConnectionMultiplexer;

namespace OrderProcessingApi.Services.Caching;

public sealed class RedisProductCacheInvalidator(RedisConnectionMultiplexer redis) : IProductCacheInvalidator
{
    public async Task InvalidateProductsAsync(CancellationToken cancellationToken)
    {
        await redis.GetDatabase().KeyDeleteAsync(ProductEndpoints.ProductsCacheKey);
    }
}

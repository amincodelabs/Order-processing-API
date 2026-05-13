using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderProcessingApi.Data;

public sealed class OrderProcessingDbContextFactory : IDesignTimeDbContextFactory<OrderProcessingDbContext>
{
    public OrderProcessingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderProcessingDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be configured.");
        }

        optionsBuilder.UseSqlServer(connectionString);

        return new OrderProcessingDbContext(optionsBuilder.Options);
    }
}

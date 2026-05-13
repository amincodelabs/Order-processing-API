using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderProcessingApi.Data;

public sealed class OrderProcessingDbContextFactory : IDesignTimeDbContextFactory<OrderProcessingDbContext>
{
    public OrderProcessingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderProcessingDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=OrderProcessingDb;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;Encrypt=False";

        optionsBuilder.UseSqlServer(connectionString);

        return new OrderProcessingDbContext(optionsBuilder.Options);
    }
}

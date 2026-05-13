using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryGrpcService.Data;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__InventoryConnection")
            ?? "Server=localhost,1433;Database=OrderProcessingDb;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;Encrypt=False";

        optionsBuilder.UseSqlServer(connectionString);

        return new InventoryDbContext(optionsBuilder.Options);
    }
}

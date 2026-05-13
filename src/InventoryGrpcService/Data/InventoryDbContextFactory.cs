using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryGrpcService.Data;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__InventoryConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings__InventoryConnection must be configured.");
        }

        optionsBuilder.UseSqlServer(connectionString);

        return new InventoryDbContext(optionsBuilder.Options);
    }
}

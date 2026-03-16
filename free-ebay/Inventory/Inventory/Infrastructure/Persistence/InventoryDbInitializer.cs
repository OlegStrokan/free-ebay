using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public sealed class InventoryDbInitializer(
    InventoryDbContext dbContext,
    ILogger<InventoryDbInitializer> logger)
{
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        logger.LogInformation("Inventory database schema ensured.");
    }
}

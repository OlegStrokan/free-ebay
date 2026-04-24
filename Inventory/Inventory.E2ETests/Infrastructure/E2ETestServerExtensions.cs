using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.E2ETests.Infrastructure;

public static class E2ETestServerExtensions
{
    public static async Task ResetAsync(this E2ETestServer server)
    {
        using var scope = server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await dbContext.InventoryMovements.ExecuteDeleteAsync();
        await dbContext.OutboxMessages.ExecuteDeleteAsync();
        await dbContext.InventoryReservations.ExecuteDeleteAsync();
        await dbContext.ProductStocks.ExecuteDeleteAsync();
    }

    public static async Task SeedStockAsync(
        this E2ETestServer server,
        Guid productId,
        int availableQuantity,
        int reservedQuantity = 0)
    {
        using var scope = server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        dbContext.ProductStocks.Add(new ProductStockEntity
        {
            ProductId = productId,
            AvailableQuantity = availableQuantity,
            ReservedQuantity = reservedQuantity,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    public static async Task<ProductStockEntity?> GetStockAsync(this E2ETestServer server, Guid productId)
    {
        using var scope = server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        return await dbContext.ProductStocks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId);
    }

    public static async Task<InventoryReservationEntity?> GetReservationAsync(
        this E2ETestServer server,
        Guid reservationId)
    {
        using var scope = server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        return await dbContext.InventoryReservations
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId);
    }
}

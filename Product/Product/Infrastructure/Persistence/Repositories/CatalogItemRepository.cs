using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class CatalogItemRepository(ProductDbContext dbContext) : ICatalogItemRepository
{
    public Task<CatalogItem?> GetByIdAsync(CatalogItemId id, CancellationToken cancellationToken = default)
        => dbContext.CatalogItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CatalogItem?> GetByGtinAsync(string gtin, CancellationToken cancellationToken = default)
        => dbContext.CatalogItems.FirstOrDefaultAsync(c => c.Gtin == gtin, cancellationToken);

    public async Task AddAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        await dbContext.CatalogItems.AddAsync(catalogItem, cancellationToken);
    }

    public Task UpdateAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        dbContext.CatalogItems.Update(catalogItem);
        return Task.CompletedTask;
    }
}
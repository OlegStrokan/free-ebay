using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class ListingRepository(ProductDbContext dbContext) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(ListingId id, CancellationToken cancellationToken = default)
        => dbContext.Listings.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> ActiveListingExistsAsync(
        CatalogItemId catalogItemId,
        SellerId sellerId,
        ListingId? excludedListingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Listings
            .Where(l => l.CatalogItemId == catalogItemId)
            .Where(l => l.SellerId == sellerId)
            .Where(l => l.Status != ListingStatus.Deleted);

        if (excludedListingId is not null)
            query = query.Where(l => l.Id != excludedListingId);

        return query.AnyAsync(cancellationToken);
    }

    public async Task<List<Listing>> GetActiveListingsForCatalogItemAsync(
        CatalogItemId catalogItemId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Listings
            .Where(l => l.CatalogItemId == catalogItemId)
            .Where(l => l.Status == ListingStatus.Active || l.Status == ListingStatus.OutOfStock)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        await dbContext.Listings.AddAsync(listing, cancellationToken);
    }

    public Task UpdateAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        dbContext.Listings.Update(listing);
        return Task.CompletedTask;
    }
}
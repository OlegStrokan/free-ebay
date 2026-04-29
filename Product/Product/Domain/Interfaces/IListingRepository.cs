using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(ListingId id, CancellationToken cancellationToken = default);
    Task<bool> ActiveListingExistsAsync(
        CatalogItemId catalogItemId,
        SellerId sellerId,
        ListingId? excludedListingId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Listing listing, CancellationToken cancellationToken = default);
    Task UpdateAsync(Listing listing, CancellationToken cancellationToken = default);
}
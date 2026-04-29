using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface ICatalogItemRepository
{
    Task<CatalogItem?> GetByIdAsync(CatalogItemId id, CancellationToken cancellationToken = default);
    Task<CatalogItem?> GetByGtinAsync(string gtin, CancellationToken cancellationToken = default);
    Task AddAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);
}
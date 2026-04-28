using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IProductPersistenceService
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task CreateProductAsync(Product product, CancellationToken ct = default);
    Task UpdateProductAsync(Product product, CancellationToken ct = default);

    Task<CatalogItem?> GetCatalogItemByIdAsync(CatalogItemId id, CancellationToken ct = default);
    Task<CatalogItem?> GetCatalogItemByGtinAsync(string gtin, CancellationToken ct = default);
    Task<Listing?> GetListingByIdAsync(ListingId id, CancellationToken ct = default);
    Task<bool> ActiveListingExistsAsync(
        CatalogItemId catalogItemId,
        SellerId sellerId,
        ListingId? excludedListingId = null,
        CancellationToken ct = default);

    Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken ct = default);
    Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken ct = default);
    Task CreateListingAsync(Listing listing, CancellationToken ct = default);
    Task UpdateListingAsync(Listing listing, CancellationToken ct = default);
    Task CreateCatalogItemWithListingAsync(CatalogItem catalogItem, Listing listing, CancellationToken ct = default);
    Task UpdateCatalogItemWithListingAsync(CatalogItem catalogItem, Listing listing, CancellationToken ct = default);
}

using Application.DTOs;

namespace Application.Interfaces;

public interface IListingReadRepository
{
    Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken ct = default);
    Task<List<ProductDetailDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<List<ProductPriceDto>> GetPricesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<PagedResult<ProductSummaryDto>> GetBySellerAsync(Guid sellerId, int page, int size, CancellationToken ct = default);
    Task<PagedResult<ProductDetailDto>> GetByCatalogItemAsync(
        Guid catalogItemId, int page, int size, string? conditionFilter, string sortBy, CancellationToken ct = default);
}

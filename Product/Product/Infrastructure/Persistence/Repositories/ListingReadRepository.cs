using Application.DTOs;
using Application.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class ListingReadRepository(ProductDbContext dbContext) : IListingReadRepository
{
    public async Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var listingId = ListingId.From(productId);
        var filtered  = dbContext.Listings.AsNoTracking().Where(l => l.Id == listingId);
        var product   = await WithCatalogItem(filtered).FirstOrDefaultAsync(ct);

        if (product is null)
            return null;

        var categoryNames = await GetCategoryNamesAsync([product.CatalogItem.CategoryId.Value], ct);
        return MapToDetail(product, categoryNames.GetValueOrDefault(product.CatalogItem.CategoryId.Value, string.Empty));
    }

    public async Task<List<ProductDetailDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var listingIds = ids.Select(ListingId.From).ToList();
        var filtered   = dbContext.Listings.AsNoTracking().Where(l => listingIds.Contains(l.Id));
        var products   = await WithCatalogItem(filtered).ToListAsync(ct);

        var categoryNames = await GetCategoryNamesAsync(
            products.Select(p => p.CatalogItem.CategoryId.Value).Distinct().ToList(), ct);

        return products
            .Select(p => MapToDetail(p, categoryNames.GetValueOrDefault(p.CatalogItem.CategoryId.Value, string.Empty)))
            .ToList();
    }

    public async Task<List<ProductPriceDto>> GetPricesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var listingIds = ids.Select(ListingId.From).ToList();

        return await dbContext.Listings
            .AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .Select(l => new ProductPriceDto(l.Id.Value, l.Price.Amount, l.Price.Currency, l.CatalogItemId.Value, l.SellerId.Value))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<ProductSummaryDto>> GetBySellerAsync(
        Guid sellerId, int page, int size, CancellationToken ct = default)
    {
        var sellerIdVo  = SellerId.From(sellerId);
        var baseListings = dbContext.Listings.AsNoTracking().Where(l => l.SellerId == sellerIdVo);

        var totalCount = await baseListings.CountAsync(ct);

        var paged = baseListings.OrderByDescending(l => l.CreatedAt).Skip((page - 1) * size).Take(size);
        var products = await WithCatalogItem(paged).ToListAsync(ct);

        var categoryNames = await GetCategoryNamesAsync(
            products.Select(p => p.CatalogItem.CategoryId.Value).Distinct().ToList(), ct);

        var items = products.Select(p => new ProductSummaryDto(
            p.Listing.Id.Value,
            p.CatalogItem.Name,
            categoryNames.GetValueOrDefault(p.CatalogItem.CategoryId.Value, string.Empty),
            p.Listing.Price.Amount,
            p.Listing.Price.Currency,
            p.Listing.StockQuantity,
            p.Listing.Status.Name,
            p.CatalogItem.Id.Value,
            p.Listing.SellerId.Value,
            p.Listing.Condition.Name)).ToList();

        return new PagedResult<ProductSummaryDto>(items, totalCount, page, size);
    }

    // Join pre-filtered listings with their catalog item. All WHERE/ORDER/SKIP/TAKE
    // must be applied to the listings source BEFORE calling this - EF cannot translate
    // predicates against the projected ListingProjection record type.
    private IQueryable<ListingProjection> WithCatalogItem(IQueryable<Domain.Entities.Listing> listings)
        => from listing in listings
           join catalogItem in dbContext.CatalogItems.AsNoTracking()
               on listing.CatalogItemId equals catalogItem.Id
           select new ListingProjection(listing, catalogItem);

    private Task<Dictionary<Guid, string>> GetCategoryNamesAsync(List<Guid> categoryIds, CancellationToken ct)
        => dbContext.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

    private static ProductDetailDto MapToDetail(ListingProjection projection, string categoryName)
        => new(
            projection.Listing.Id.Value,
            projection.CatalogItem.Name,
            projection.CatalogItem.Description,
            projection.CatalogItem.CategoryId.Value,
            categoryName,
            projection.Listing.Price.Amount,
            projection.Listing.Price.Currency,
            projection.Listing.StockQuantity,
            projection.Listing.Status.Name,
            projection.Listing.SellerId.Value,
            projection.CatalogItem.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
            projection.CatalogItem.ImageUrls.ToList(),
            projection.Listing.CreatedAt,
            projection.Listing.UpdatedAt,
            projection.CatalogItem.Id.Value,
            projection.CatalogItem.Gtin,
            projection.Listing.Condition.Name,
            projection.Listing.SellerNotes);

    private sealed record ListingProjection(
        Domain.Entities.Listing Listing,
        Domain.Entities.CatalogItem CatalogItem);
}

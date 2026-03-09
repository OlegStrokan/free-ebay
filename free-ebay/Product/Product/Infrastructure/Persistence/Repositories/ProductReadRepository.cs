using Application.DTOs;
using Application.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class ProductReadRepository(ProductDbContext dbContext) : IProductReadRepository
{
    public async Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == ProductId.From(productId), ct);

        if (product is null)
            return null;

        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == product.CategoryId.Value, ct);

        return new ProductDetailDto(
            product.Id.Value,
            product.Name,
            product.Description,
            product.CategoryId.Value,
            category?.Name ?? string.Empty,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity,
            product.Status.Name,
            product.SellerId.Value,
            product.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
            product.ImageUrls.ToList(),
            product.CreatedAt,
            product.UpdatedAt);
    }

    public async Task<List<ProductDetailDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var guidList = ids.ToList();
        var productIds = guidList.Select(ProductId.From).ToList();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        if (products.Count == 0)
            return [];

        var categoryIds = products.Select(p => p.CategoryId.Value).Distinct().ToList();
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return products.Select(p => new ProductDetailDto(
            p.Id.Value,
            p.Name,
            p.Description,
            p.CategoryId.Value,
            categories.GetValueOrDefault(p.CategoryId.Value, string.Empty),
            p.Price.Amount,
            p.Price.Currency,
            p.StockQuantity,
            p.Status.Name,
            p.SellerId.Value,
            p.Attributes.Select(a => new ProductAttributeDto(a.Key, a.Value)).ToList(),
            p.ImageUrls.ToList(),
            p.CreatedAt,
            p.UpdatedAt)).ToList();
    }

    public async Task<List<ProductPriceDto>> GetPricesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var productIds = ids.Select(ProductId.From).ToList();

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductPriceDto(p.Id.Value, p.Price.Amount, p.Price.Currency))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<ProductSummaryDto>> GetBySellerAsync(
        Guid sellerId, int page, int size, CancellationToken ct = default)
    {
        var sellerIdVo = SellerId.From(sellerId);

        var query = dbContext.Products
            .AsNoTracking()
            .Where(p => p.SellerId == sellerIdVo);

        var totalCount = await query.CountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var categoryIds = products.Select(p => p.CategoryId.Value).Distinct().ToList();
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var items = products.Select(p => new ProductSummaryDto(
            p.Id.Value,
            p.Name,
            categories.GetValueOrDefault(p.CategoryId.Value, string.Empty),
            p.Price.Amount,
            p.Price.Currency,
            p.StockQuantity,
            p.Status.Name)).ToList();

        return new PagedResult<ProductSummaryDto>(items, totalCount, page, size);
    }
}

using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetProductPrices;

internal sealed class GetProductPricesQueryHandler(IProductReadRepository repository)
    : IRequestHandler<GetProductPricesQuery, Result<List<ProductPriceDto>>>
{
    public async Task<Result<List<ProductPriceDto>>> Handle(
        GetProductPricesQuery request, CancellationToken cancellationToken)
    {
        var prices = await repository.GetPricesByIdsAsync(request.ProductIds, cancellationToken);

        var foundIds = prices.Select(p => p.ProductId).ToHashSet();
        var missing  = request.ProductIds.Except(foundIds).ToList();
        if (missing.Count > 0)
            return Result<List<ProductPriceDto>>.Failure(
                $"Products not found: {string.Join(", ", missing)}");

        return Result<List<ProductPriceDto>>.Success(prices);
    }
}

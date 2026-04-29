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
        return Result<List<ProductPriceDto>>.Success(prices);
    }
}

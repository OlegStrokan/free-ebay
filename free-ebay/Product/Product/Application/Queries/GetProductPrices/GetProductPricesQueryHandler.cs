using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetProductPrices;

internal sealed class GetProductPricesQueryHandler : IRequestHandler<GetProductPricesQuery, Result<List<ProductPriceDto>>>
{
    private readonly IProductReadRepository _readRepository;

    public GetProductPricesQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<List<ProductPriceDto>>> Handle(GetProductPricesQuery request, CancellationToken cancellationToken)
    {
        var prices = await _readRepository.GetPricesByIdsAsync(request.ProductIds, cancellationToken);
        return Result<List<ProductPriceDto>>.Success(prices);
    }
}

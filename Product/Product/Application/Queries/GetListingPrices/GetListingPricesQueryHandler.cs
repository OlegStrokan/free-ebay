using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetListingPrices;

internal sealed class GetListingPricesQueryHandler : IRequestHandler<GetListingPricesQuery, Result<List<ProductPriceDto>>>
{
    private readonly IListingReadRepository _readRepository;

    public GetListingPricesQueryHandler(IListingReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<List<ProductPriceDto>>> Handle(GetListingPricesQuery request, CancellationToken cancellationToken)
    {
        var prices = await _readRepository.GetPricesByIdsAsync(request.ListingIds, cancellationToken);
        return Result<List<ProductPriceDto>>.Success(prices);
    }
}

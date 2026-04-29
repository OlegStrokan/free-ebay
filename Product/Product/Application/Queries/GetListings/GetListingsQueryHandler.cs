using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetListings;

internal sealed class GetListingsQueryHandler : IRequestHandler<GetListingsQuery, Result<List<ProductDetailDto>>>
{
    private readonly IListingReadRepository _readRepository;

    public GetListingsQueryHandler(IListingReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<List<ProductDetailDto>>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        var listings = await _readRepository.GetByIdsAsync(request.ListingIds, cancellationToken);
        return Result<List<ProductDetailDto>>.Success(listings);
    }
}

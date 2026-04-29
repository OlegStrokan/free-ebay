using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetSellerListings;

internal sealed class GetSellerListingsQueryHandler
    : IRequestHandler<GetSellerListingsQuery, Result<PagedResult<ProductSummaryDto>>>
{
    private readonly IListingReadRepository _readRepository;

    public GetSellerListingsQueryHandler(IListingReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<ProductSummaryDto>>> Handle(
        GetSellerListingsQuery request, CancellationToken cancellationToken)
    {
        var result = await _readRepository.GetBySellerAsync(
            request.SellerId, request.Page, request.Size, cancellationToken);

        return Result<PagedResult<ProductSummaryDto>>.Success(result);
    }
}

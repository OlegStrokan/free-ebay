using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetSellerProducts;

internal sealed class GetSellerProductsQueryHandler
    : IRequestHandler<GetSellerProductsQuery, Result<PagedResult<ProductSummaryDto>>>
{
    private readonly IProductReadRepository _readRepository;

    public GetSellerProductsQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<ProductSummaryDto>>> Handle(
        GetSellerProductsQuery request, CancellationToken cancellationToken)
    {
        var result = await _readRepository.GetBySellerAsync(
            request.SellerId, request.Page, request.Size, cancellationToken);

        return Result<PagedResult<ProductSummaryDto>>.Success(result);
    }
}

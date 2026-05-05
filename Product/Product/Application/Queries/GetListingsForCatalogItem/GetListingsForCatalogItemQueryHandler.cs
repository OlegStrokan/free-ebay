using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetListingsForCatalogItem;

internal sealed class GetListingsForCatalogItemQueryHandler(IListingReadRepository readRepository)
    : IRequestHandler<GetListingsForCatalogItemQuery, Result<PagedResult<ProductDetailDto>>>
{
    public async Task<Result<PagedResult<ProductDetailDto>>> Handle(
        GetListingsForCatalogItemQuery request, CancellationToken cancellationToken)
    {
        var result = await readRepository.GetByCatalogItemAsync(
            request.CatalogItemId, request.Page, request.Size,
            request.ConditionFilter, request.SortBy, cancellationToken);

        return Result<PagedResult<ProductDetailDto>>.Success(result);
    }
}

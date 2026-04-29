using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetProducts;

internal sealed class GetProductsQueryHandler(IProductReadRepository repository)
    : IRequestHandler<GetProductsQuery, Result<List<ProductDetailDto>>>
{
    public async Task<Result<List<ProductDetailDto>>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await repository.GetByIdsAsync(request.ProductIds, cancellationToken);
        return Result<List<ProductDetailDto>>.Success(products);
    }
}

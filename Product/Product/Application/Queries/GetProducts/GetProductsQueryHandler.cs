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

        var foundIds = products.Select(p => p.ProductId).ToHashSet();
        var missing  = request.ProductIds.Except(foundIds).ToList();
        if (missing.Count > 0)
            return Result<List<ProductDetailDto>>.Failure(
                $"Products not found: {string.Join(", ", missing)}");

        return Result<List<ProductDetailDto>>.Success(products);
    }
}

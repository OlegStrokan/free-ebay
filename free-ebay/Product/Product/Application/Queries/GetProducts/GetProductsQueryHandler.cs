using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetProducts;

internal sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<List<ProductDetailDto>>>
{
    private readonly IProductReadRepository _readRepository;

    public GetProductsQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<List<ProductDetailDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _readRepository.GetByIdsAsync(request.ProductIds, cancellationToken);
        return Result<List<ProductDetailDto>>.Success(products);
    }
}

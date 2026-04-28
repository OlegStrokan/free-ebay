using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Queries.GetProduct;

internal sealed class GetProductQueryHandler(IProductReadRepository repository)
    : IRequestHandler<GetProductQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(request.ProductId);

        return product;
    }
}

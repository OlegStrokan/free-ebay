using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Queries.GetProduct;

internal sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDetailDto>
{
    private readonly IProductReadRepository _readRepository;

    public GetProductQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<ProductDetailDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _readRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
            throw new ProductNotFoundException(request.ProductId);

        return product;
    }
}

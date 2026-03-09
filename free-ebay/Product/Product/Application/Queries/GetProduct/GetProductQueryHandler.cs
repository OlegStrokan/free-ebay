using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.GetProduct;

internal sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, Result<ProductDetailDto>>
{
    private readonly IProductReadRepository _readRepository;

    public GetProductQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<ProductDetailDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _readRepository.GetByIdAsync(request.ProductId, cancellationToken);

        return product is null
            ? Result<ProductDetailDto>.Failure($"Product with ID {request.ProductId} was not found.")
            : Result<ProductDetailDto>.Success(product);
    }
}

using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.CreateProduct;

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductPersistenceService _persistence;

    public CreateProductCommandHandler(IProductPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var sellerId   = SellerId.From(request.SellerId);
            var categoryId = CategoryId.From(request.CategoryId);
            var price      = Money.Create(request.Price, request.Currency);
            var attributes = request.Attributes
                .Select(a => new ProductAttribute(a.Key, a.Value))
                .ToList();

            var product = Product.Create(
                sellerId,
                request.Name,
                request.Description,
                categoryId,
                price,
                request.InitialStock,
                attributes,
                request.ImageUrls);

            await _persistence.CreateProductAsync(product, cancellationToken);

            return Result<Guid>.Success(product.Id.Value);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

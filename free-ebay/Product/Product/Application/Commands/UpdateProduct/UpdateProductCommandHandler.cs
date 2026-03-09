using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IProductPersistenceService _persistence;

    public UpdateProductCommandHandler(IProductPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product   = await _persistence.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            var categoryId = CategoryId.From(request.CategoryId);
            var price      = Money.Create(request.Price, request.Currency);
            var attributes = request.Attributes
                .Select(a => new ProductAttribute(a.Key, a.Value))
                .ToList();

            product.Update(request.Name, request.Description, categoryId, price, attributes, request.ImageUrls);

            await _persistence.UpdateProductAsync(product, cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

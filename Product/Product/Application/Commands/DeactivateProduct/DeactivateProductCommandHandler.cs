using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.DeactivateProduct;

internal sealed class DeactivateProductCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<DeactivateProductCommand, Result>
{
    public async Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await persistence.GetByIdAsync(ProductId.From(request.ProductId), cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.Deactivate();
            await persistence.UpdateProductAsync(product, cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.DeactivateProduct;

internal sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand, Result>
{
    private readonly IProductPersistenceService _persistence;

    public DeactivateProductCommandHandler(IProductPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product   = await _persistence.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.Deactivate();

            await _persistence.UpdateProductAsync(product, cancellationToken);

            return Result.Success();
        }
        catch (InvalidProductOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

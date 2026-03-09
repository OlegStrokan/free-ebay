using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.ActivateProduct;

internal sealed class ActivateProductCommandHandler : IRequestHandler<ActivateProductCommand, Result>
{
    private readonly IProductPersistenceService _persistence;

    public ActivateProductCommandHandler(IProductPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<Result> Handle(ActivateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product   = await _persistence.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.Activate();

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

using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.AdjustProductStock;

internal sealed class AdjustProductStockCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<AdjustProductStockCommand, Result>
{
    public async Task<Result> Handle(AdjustProductStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product = await persistence.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.AdjustStock(request.Delta);

            await persistence.UpdateProductAsync(product, cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

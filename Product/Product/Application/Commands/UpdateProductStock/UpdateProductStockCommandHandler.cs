using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.UpdateProductStock;

internal sealed class UpdateProductStockCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<UpdateProductStockCommand, Result>
{
    public async Task<Result> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await persistence.GetByIdAsync(ProductId.From(request.ProductId), cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.UpdateStock(request.NewQuantity);
            await persistence.UpdateProductAsync(product, cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

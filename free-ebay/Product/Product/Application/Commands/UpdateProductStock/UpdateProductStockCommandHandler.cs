using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.UpdateProductStock;

internal sealed class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, Result>
{
    private readonly IProductPersistenceService _persistence;

    public UpdateProductStockCommandHandler(IProductPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<Result> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product   = await _persistence.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.UpdateStock(request.NewQuantity);

            await _persistence.UpdateProductAsync(product, cancellationToken);

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

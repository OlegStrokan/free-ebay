using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.DeleteProduct;

internal sealed class DeleteProductCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);
            var product = await persistence.GetByIdAsync(productId, cancellationToken);
            if (product is null)
                return Result.Failure($"Product with ID {request.ProductId} was not found.");

            product.Delete();

            await persistence.UpdateProductAsync(product, cancellationToken);

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
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

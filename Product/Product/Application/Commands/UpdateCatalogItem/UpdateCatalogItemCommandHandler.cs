using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.UpdateCatalogItem;

internal sealed class UpdateCatalogItemCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<UpdateCatalogItemCommand, Result>
{
    public async Task<Result> Handle(UpdateCatalogItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var catalogItemId = CatalogItemId.From(request.CatalogItemId);
            var catalogItem = await persistence.GetCatalogItemByIdAsync(catalogItemId, cancellationToken);
            if (catalogItem is null)
                return Result.Failure($"Catalog item with ID {request.CatalogItemId} was not found.");

            if (!string.IsNullOrWhiteSpace(request.Gtin))
            {
                var gtinOwner = await persistence.GetCatalogItemByGtinAsync(request.Gtin.Trim(), cancellationToken);
                if (gtinOwner is not null && !gtinOwner.Id.Equals(catalogItem.Id))
                    return Result.Failure($"Catalog item with GTIN {request.Gtin.Trim()} already exists.");
            }

            var attributes = request.Attributes
                .Select(a => new ProductAttribute(a.Key, a.Value))
                .ToList();

            catalogItem.Update(
                request.Name,
                request.Description,
                CategoryId.From(request.CategoryId),
                request.Gtin,
                attributes,
                request.ImageUrls);

            await persistence.UpdateCatalogItemAsync(catalogItem, cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
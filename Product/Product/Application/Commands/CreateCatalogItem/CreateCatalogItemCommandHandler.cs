using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.CreateCatalogItem;

internal sealed class CreateCatalogItemCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<CreateCatalogItemCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCatalogItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Gtin)
                && await persistence.GetCatalogItemByGtinAsync(request.Gtin.Trim(), cancellationToken) is not null)
                return Result<Guid>.Failure($"Catalog item with GTIN {request.Gtin.Trim()} already exists.");

            var attributes = request.Attributes
                .Select(a => new ProductAttribute(a.Key, a.Value))
                .ToList();

            var catalogItem = CatalogItem.Create(
                request.Name,
                request.Description,
                CategoryId.From(request.CategoryId),
                request.Gtin,
                attributes,
                request.ImageUrls);

            await persistence.CreateCatalogItemAsync(catalogItem, cancellationToken);

            return Result<Guid>.Success(catalogItem.Id.Value);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
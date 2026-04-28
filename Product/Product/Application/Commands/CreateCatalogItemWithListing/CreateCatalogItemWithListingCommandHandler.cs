using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.CreateCatalogItemWithListing;

internal sealed class CreateCatalogItemWithListingCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<CreateCatalogItemWithListingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCatalogItemWithListingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Gtin)
                && await persistence.GetCatalogItemByGtinAsync(request.Gtin.Trim(), cancellationToken) is not null)
                return Result<Guid>.Failure($"Catalog item with GTIN {request.Gtin.Trim()} already exists.");

            var sellerId = SellerId.From(request.SellerId);
            var categoryId = CategoryId.From(request.CategoryId);
            var price = Money.Create(request.Price, request.Currency);
            var condition = ListingCondition.FromName(request.Condition);
            var attributes = request.Attributes.Select(a => new ProductAttribute(a.Key, a.Value)).ToList();

            var catalogItem = CatalogItem.Create(
                request.Name, request.Description, categoryId, request.Gtin, attributes, request.ImageUrls);

            var listing = Listing.Create(
                catalogItem.Id, sellerId, price, request.InitialStock, condition, request.SellerNotes);

            await persistence.CreateCatalogItemWithListingAsync(catalogItem, listing, cancellationToken);

            return Result<Guid>.Success(listing.Id.Value);
        }
        catch (DomainException ex) { return Result<Guid>.Failure(ex.Message); }
    }
}

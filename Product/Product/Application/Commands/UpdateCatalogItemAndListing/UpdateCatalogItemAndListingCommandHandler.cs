using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.UpdateCatalogItemAndListing;

internal sealed class UpdateCatalogItemAndListingCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<UpdateCatalogItemAndListingCommand, Result>
{
    public async Task<Result> Handle(
        UpdateCatalogItemAndListingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var listingId = ListingId.From(request.ListingId);
            var listing = await persistence.GetListingByIdAsync(listingId, cancellationToken);
            if (listing is null)
                return Result.Failure($"Listing with ID {request.ListingId} was not found.");

            var catalogItem = await persistence.GetCatalogItemByIdAsync(listing.CatalogItemId, cancellationToken);
            if (catalogItem is null)
                return Result.Failure($"Catalog item with ID {listing.CatalogItemId.Value} was not found.");

            if (!string.IsNullOrWhiteSpace(request.Gtin))
            {
                var gtinOwner = await persistence.GetCatalogItemByGtinAsync(request.Gtin.Trim(), cancellationToken);
                if (gtinOwner is not null && !gtinOwner.Id.Equals(catalogItem.Id))
                    return Result.Failure($"Catalog item with GTIN {request.Gtin.Trim()} already exists.");
            }

            var categoryId = CategoryId.From(request.CategoryId);
            var price = Money.Create(request.Price, request.Currency);
            var condition  = request.Condition is null
                ? listing.Condition
                : ListingCondition.FromName(request.Condition);
            var attributes = request.Attributes
                .Select(a => new Domain.ValueObjects.ProductAttribute(a.Key, a.Value))
                .ToList();

            catalogItem.Update(request.Name, request.Description, categoryId, request.Gtin, attributes, request.ImageUrls);
            listing.ChangePrice(price);
            listing.UpdateOfferDetails(condition, request.SellerNotes);

            await persistence.UpdateCatalogItemWithListingAsync(catalogItem, listing, cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

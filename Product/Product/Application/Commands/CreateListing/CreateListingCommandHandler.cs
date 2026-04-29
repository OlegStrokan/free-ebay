using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.CreateListing;

internal sealed class CreateListingCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<CreateListingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var catalogItemId = CatalogItemId.From(request.CatalogItemId);
            if (await persistence.GetCatalogItemByIdAsync(catalogItemId, cancellationToken) is null)
                return Result<Guid>.Failure($"Catalog item with ID {request.CatalogItemId} was not found.");

            var sellerId = SellerId.From(request.SellerId);
            if (await persistence.ActiveListingExistsAsync(catalogItemId, sellerId, null, cancellationToken))
                return Result<Guid>.Failure(
                    $"Seller {request.SellerId} already has a non-deleted listing for catalog item {request.CatalogItemId}.");

            var listing = Listing.Create(
                catalogItemId,
                sellerId,
                Money.Create(request.Price, request.Currency),
                request.InitialStock,
                ListingCondition.FromName(request.Condition),
                request.SellerNotes);

            await persistence.CreateListingAsync(listing, cancellationToken);

            return Result<Guid>.Success(listing.Id.Value);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
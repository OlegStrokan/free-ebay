using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.UpdateListingStock;

internal sealed class UpdateListingStockCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<UpdateListingStockCommand, Result>
{
    public async Task<Result> Handle(UpdateListingStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var listing = await persistence.GetListingByIdAsync(ListingId.From(request.ListingId), cancellationToken);
            if (listing is null)
                return Result.Failure($"Listing with ID {request.ListingId} was not found.");

            listing.UpdateStock(request.NewQuantity);
            await persistence.UpdateListingAsync(listing, cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

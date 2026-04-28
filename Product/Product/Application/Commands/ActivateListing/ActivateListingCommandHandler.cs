using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.ActivateListing;

internal sealed class ActivateListingCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<ActivateListingCommand, Result>
{
    public async Task<Result> Handle(ActivateListingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var listing = await persistence.GetListingByIdAsync(ListingId.From(request.ListingId), cancellationToken);
            if (listing is null)
                return Result.Failure($"Listing with ID {request.ListingId} was not found.");

            listing.Activate();
            await persistence.UpdateListingAsync(listing, cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

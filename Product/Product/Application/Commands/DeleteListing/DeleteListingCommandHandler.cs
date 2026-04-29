using Application.Common;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.DeleteListing;

internal sealed class DeleteListingCommandHandler(IProductPersistenceService persistence)
    : IRequestHandler<DeleteListingCommand, Result>
{
    public async Task<Result> Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var listing = await persistence.GetListingByIdAsync(ListingId.From(request.ListingId), cancellationToken);
            if (listing is null)
                return Result.Failure($"Listing with ID {request.ListingId} was not found.");

            listing.Delete();
            await persistence.UpdateListingAsync(listing, cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

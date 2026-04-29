using Application.Common;

namespace Application.Commands.DeleteListing;

public sealed record DeleteListingCommand(Guid ListingId) : ICommand<Result>;

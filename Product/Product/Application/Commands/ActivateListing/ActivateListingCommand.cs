using Application.Common;

namespace Application.Commands.ActivateListing;

public sealed record ActivateListingCommand(Guid ListingId) : ICommand<Result>;

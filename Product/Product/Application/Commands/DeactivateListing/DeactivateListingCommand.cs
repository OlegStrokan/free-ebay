using Application.Common;

namespace Application.Commands.DeactivateListing;

public sealed record DeactivateListingCommand(Guid ListingId) : ICommand<Result>;

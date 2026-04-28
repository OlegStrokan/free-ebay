using Application.Common;

namespace Application.Commands.ChangeListingPrice;

public sealed record ChangeListingPriceCommand(Guid ListingId, decimal Price, string Currency) : ICommand<Result>;
using Application.Common;

namespace Application.Commands.AdjustListingStock;

public sealed record AdjustListingStockCommand(Guid ListingId, int Delta) : ICommand<Result>;

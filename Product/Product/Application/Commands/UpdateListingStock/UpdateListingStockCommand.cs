using Application.Common;

namespace Application.Commands.UpdateListingStock;

public sealed record UpdateListingStockCommand(Guid ListingId, int NewQuantity) : ICommand<Result>;

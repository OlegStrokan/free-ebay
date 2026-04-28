using Application.Common;

namespace Application.Commands.CreateListing;

public sealed record CreateListingCommand(
    Guid CatalogItemId,
    Guid SellerId,
    decimal Price,
    string Currency,
    int InitialStock,
    string Condition,
    string? SellerNotes) : ICommand<Result<Guid>>;
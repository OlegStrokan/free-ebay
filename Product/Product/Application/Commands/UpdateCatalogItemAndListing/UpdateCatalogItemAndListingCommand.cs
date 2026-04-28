using Application.Common;
using Application.DTOs;

namespace Application.Commands.UpdateCatalogItemAndListing;

public sealed record UpdateCatalogItemAndListingCommand(
    Guid ListingId,
    string Name,
    string Description,
    Guid CategoryId,
    decimal Price,
    string Currency,
    List<ProductAttributeDto> Attributes,
    List<string> ImageUrls,
    string? Gtin,
    string? Condition,
    string? SellerNotes) : ICommand<Result>;

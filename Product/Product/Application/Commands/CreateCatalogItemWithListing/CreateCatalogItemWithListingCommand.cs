using Application.Common;
using Application.DTOs;

namespace Application.Commands.CreateCatalogItemWithListing;

public sealed record CreateCatalogItemWithListingCommand(
    Guid SellerId,
    string Name,
    string Description,
    Guid CategoryId,
    decimal Price,
    string Currency,
    int InitialStock,
    List<ProductAttributeDto> Attributes,
    List<string> ImageUrls,
    string? Gtin,
    string Condition,
    string? SellerNotes) : ICommand<Result<Guid>>;

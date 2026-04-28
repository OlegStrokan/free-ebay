using Application.Common;
using Application.DTOs;

namespace Application.Commands.UpdateCatalogItem;

public sealed record UpdateCatalogItemCommand(
    Guid CatalogItemId,
    string Name,
    string Description,
    Guid CategoryId,
    string? Gtin,
    List<ProductAttributeDto> Attributes,
    List<string> ImageUrls) : ICommand<Result>;
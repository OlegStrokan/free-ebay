using Application.Common;
using Application.DTOs;

namespace Application.Commands.CreateCatalogItem;

public sealed record CreateCatalogItemCommand(
    string Name,
    string Description,
    Guid CategoryId,
    string? Gtin,
    List<ProductAttributeDto> Attributes,
    List<string> ImageUrls) : ICommand<Result<Guid>>;
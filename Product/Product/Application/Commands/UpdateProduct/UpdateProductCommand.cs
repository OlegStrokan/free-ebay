using Application.Common;
using Application.DTOs;

namespace Application.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    decimal Price,
    string Currency,
    List<ProductAttributeDto> Attributes,
    List<string> ImageUrls) : ICommand<Result>;

namespace Application.DTOs;

public sealed record ProductPriceDto(Guid ProductId, decimal Price, string Currency);

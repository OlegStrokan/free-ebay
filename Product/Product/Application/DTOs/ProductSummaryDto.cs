namespace Application.DTOs;

public sealed record ProductSummaryDto(
    Guid ProductId,
    string Name,
    string CategoryName,
    decimal Price,
    string Currency,
    int StockQuantity,
    string Status,
    Guid CatalogItemId,
    Guid SellerId,
    string Condition);

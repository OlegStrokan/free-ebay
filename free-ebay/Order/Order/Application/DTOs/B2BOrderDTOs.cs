namespace Application.DTOs;

public record QuoteItemChangeDto(
    QuoteChangeType Type,
    Guid? ProductId,
    int? Quantity,
    decimal? Price,
    string? Currency
);

public enum QuoteChangeType
{
    AddItem,
    RemoveItem,
    ChangeQuantity,
    AdjustItemPrice
}

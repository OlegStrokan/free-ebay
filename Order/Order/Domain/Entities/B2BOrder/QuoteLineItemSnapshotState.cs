namespace Domain.Entities.B2BOrder;

public record QuoteLineItemSnapshotState(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal? AdjustedUnitPrice,
    string Currency,
    bool IsRemoved);

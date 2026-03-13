namespace Application.Models;

// Partial Elasticsearch update applied on ProductStockUpdatedEvent.
// Only Stock is written; all other fields in the stored document are preserved.
public sealed class StockUpdateDocument
{
    public required int Stock { get; init; }
}

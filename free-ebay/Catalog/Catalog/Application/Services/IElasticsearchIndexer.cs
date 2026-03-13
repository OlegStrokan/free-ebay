using Application.Models;

namespace Application.Services;

public interface IElasticsearchIndexer
{
    Task UpsertAsync(ProductSearchDocument document, CancellationToken ct = default);

    Task UpdateFieldsAsync(string productId, ProductFieldsUpdateDocument update, CancellationToken ct = default);

    Task UpdateStockAsync(string productId, StockUpdateDocument update, CancellationToken ct = default);

    Task UpdateStatusAsync(string productId, StatusUpdateDocument update, CancellationToken ct = default);

    Task DeleteAsync(string productId, CancellationToken ct = default);
}

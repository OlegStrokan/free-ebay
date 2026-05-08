namespace Application.Gateways;

public interface IAiSimilarItemsGateway
{
    Task<SimilarItemsResult> GetSimilarItemsAsync(
        string catalogItemId, int limit, string? category, string? condition, CancellationToken ct);
}

public sealed record SimilarItemResult(string CatalogItemId, double Score);

public sealed record SimilarItemsResult(List<SimilarItemResult> Items);

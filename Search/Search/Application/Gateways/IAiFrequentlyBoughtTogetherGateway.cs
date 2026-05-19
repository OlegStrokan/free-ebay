namespace Application.Gateways;

public interface IAiFrequentlyBoughtTogetherGateway
{
    Task<FrequentlyBoughtTogetherResult> GetFrequentlyBoughtTogetherAsync(
        string catalogItemId, int limit, CancellationToken ct);
}

public sealed record FrequentlyBoughtTogetherItemResult(string CatalogItemId, double Score);

public sealed record FrequentlyBoughtTogetherResult(List<FrequentlyBoughtTogetherItemResult> Items);

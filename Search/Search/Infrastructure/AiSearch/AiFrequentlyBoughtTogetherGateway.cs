using Application.Gateways;
using Microsoft.Extensions.Logging;
using Protos.AiSearch;

namespace Infrastructure.AiSearch;

public sealed class AiFrequentlyBoughtTogetherGateway(
    AiSearchService.AiSearchServiceClient grpcClient,
    ILogger<AiFrequentlyBoughtTogetherGateway> logger)
    : IAiFrequentlyBoughtTogetherGateway
{
    public async Task<FrequentlyBoughtTogetherResult> GetFrequentlyBoughtTogetherAsync(
        string catalogItemId, int limit, CancellationToken ct)
    {
        var request = new AiGetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = catalogItemId,
            Limit = limit,
        };

        var response = await grpcClient.GetFrequentlyBoughtTogetherAsync(request, cancellationToken: ct);

        var items = response.Items
            .Select(i => new FrequentlyBoughtTogetherItemResult(i.CatalogItemId, i.Score))
            .ToList();

        logger.LogDebug(
            "Frequently bought together returned {Count} items for catalog item [{CatalogItemId}].",
            items.Count,
            catalogItemId);

        return new FrequentlyBoughtTogetherResult(items);
    }
}

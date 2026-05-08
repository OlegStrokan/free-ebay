using Application.Gateways;
using Microsoft.Extensions.Logging;
using Protos.AiSearch;

namespace Infrastructure.AiSearch;

public sealed class AiSimilarItemsGateway(
    AiSearchService.AiSearchServiceClient grpcClient,
    ILogger<AiSimilarItemsGateway> logger)
    : IAiSimilarItemsGateway
{
    public async Task<SimilarItemsResult> GetSimilarItemsAsync(
        string catalogItemId, int limit, string? category, string? condition, CancellationToken ct)
    {
        var request = new AiGetSimilarItemsRequest
        {
            CatalogItemId = catalogItemId,
            Limit = limit,
            Category = category ?? string.Empty,
            Condition = condition ?? string.Empty
        };

        var response = await grpcClient.GetSimilarItemsAsync(request, cancellationToken: ct);

        var items = response.Items
            .Select(i => new SimilarItemResult(i.CatalogItemId, i.Score))
            .ToList();

        logger.LogDebug(
            "AI similar items returned {Count} items for catalog item [{CatalogItemId}].",
            items.Count,
            catalogItemId);

        return new SimilarItemsResult(items);
    }
}

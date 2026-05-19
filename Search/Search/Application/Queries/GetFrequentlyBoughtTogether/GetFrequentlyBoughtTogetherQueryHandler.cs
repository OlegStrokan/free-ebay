using Application.Gateways;
using Domain.Common.Interfaces;

namespace Application.Queries.GetFrequentlyBoughtTogether;

public sealed class GetFrequentlyBoughtTogetherQueryHandler(
    IAiFrequentlyBoughtTogetherGateway gateway)
    : IQueryHandler<GetFrequentlyBoughtTogetherQuery, GetFrequentlyBoughtTogetherResult>
{
    public async Task<GetFrequentlyBoughtTogetherResult> HandleAsync(
        GetFrequentlyBoughtTogetherQuery query, CancellationToken ct)
    {
        var result = await gateway.GetFrequentlyBoughtTogetherAsync(
            query.CatalogItemId,
            query.Limit,
            ct);

        var items = result.Items
            .Select(i => new CoOccurrenceItemDto(i.CatalogItemId, i.Score))
            .ToList();

        return new GetFrequentlyBoughtTogetherResult(items);
    }
}

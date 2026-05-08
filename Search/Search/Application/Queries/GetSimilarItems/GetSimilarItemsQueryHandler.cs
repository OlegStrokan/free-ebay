using Application.Gateways;
using Domain.Common.Interfaces;

namespace Application.Queries.GetSimilarItems;

public sealed class GetSimilarItemsQueryHandler(
    IAiSimilarItemsGateway gateway)
    : IQueryHandler<GetSimilarItemsQuery, GetSimilarItemsResult>
{
    public async Task<GetSimilarItemsResult> HandleAsync(
        GetSimilarItemsQuery query, CancellationToken ct)
    {
        var result = await gateway.GetSimilarItemsAsync(
            query.CatalogItemId,
            query.Limit,
            query.Category,
            query.Condition,
            ct);

        var items = result.Items
            .Select(i => new SimilarItemDto(i.CatalogItemId, i.Score))
            .ToList();

        return new GetSimilarItemsResult(items);
    }
}

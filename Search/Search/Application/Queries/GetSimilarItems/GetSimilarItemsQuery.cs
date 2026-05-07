using Domain.Common.Interfaces;

namespace Application.Queries.GetSimilarItems;

public sealed record GetSimilarItemsQuery(
    string CatalogItemId,
    int Limit,
    string? Category,
    string? Condition) : IQuery<GetSimilarItemsResult>;

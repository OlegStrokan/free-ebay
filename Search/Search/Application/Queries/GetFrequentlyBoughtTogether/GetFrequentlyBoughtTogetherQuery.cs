using Domain.Common.Interfaces;

namespace Application.Queries.GetFrequentlyBoughtTogether;

public sealed record GetFrequentlyBoughtTogetherQuery(
    string CatalogItemId,
    int Limit) : IQuery<GetFrequentlyBoughtTogetherResult>;

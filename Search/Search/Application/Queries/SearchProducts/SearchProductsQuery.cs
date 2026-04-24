using Domain.Common.Interfaces;

namespace Application.Queries.SearchProducts;

public record SearchProductsQuery(
    string QueryText,
    bool UseAi,
    int Page,
    int Size
    ) : IQuery<SearchProductsResult>;
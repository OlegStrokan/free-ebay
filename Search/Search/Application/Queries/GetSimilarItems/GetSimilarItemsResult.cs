namespace Application.Queries.GetSimilarItems;

public sealed record SimilarItemDto(string CatalogItemId, double Score);

public sealed record GetSimilarItemsResult(List<SimilarItemDto> Items);

namespace Application.Queries.GetFrequentlyBoughtTogether;

public sealed record CoOccurrenceItemDto(string CatalogItemId, double Score);

public sealed record GetFrequentlyBoughtTogetherResult(List<CoOccurrenceItemDto> Items);

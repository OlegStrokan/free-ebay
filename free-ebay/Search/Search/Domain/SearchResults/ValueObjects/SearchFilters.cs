namespace Domain.SearchResults.ValueObjects;

public sealed record SearchFilters(
    decimal? PriceMin,
    decimal? PriceMax,
    string? Category,
    string? Color,
    string? Brand,
    string? Layout
    );
namespace Application.Models;

public sealed class ListingSummaryUpdateDocument
{
    public required decimal MinPrice { get; init; }
    public required string MinPriceCurrency { get; init; }
    public required int SellerCount { get; init; }
    public required bool HasActiveListings { get; init; }
    public required string? BestCondition { get; init; }
    public required int TotalStock { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

namespace Application.Models;

public sealed class CatalogItemFieldsUpdateDocument
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string CategoryId { get; init; }
    public required Dictionary<string, string> Attributes { get; init; }
    public required List<string> ImageUrls { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

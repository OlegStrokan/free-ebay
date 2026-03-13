namespace Application.Models;

// Sent as a partial document on ProductUpdatedEvent.
// Stock and status fields in Elasticsearch are untouched by this update.
public sealed class ProductFieldsUpdateDocument
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string CategoryId { get; init; }
    public required decimal Price { get; init; }
    public required string Currency { get; init; }
    public required Dictionary<string, string> Attributes { get; init; }
    public required List<string> ImageUrls { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

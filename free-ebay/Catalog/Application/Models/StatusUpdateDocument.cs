namespace Application.Models;

// Partial Elasticsearch update applied on ProductStatusChangedEvent.
// Only Status is written; all other fields in the stored document are preserved.
public sealed class StatusUpdateDocument
{
    public required string Status { get; init; }
}

using Domain.Entities;

namespace Application.Interfaces;

// repo for messages that failed to process after max retries
// or exceeded maximum age. These require manual investigation
public interface IDeadLetterRepository
{
    Task AddAsync(
        Guid messageId,
        string type,
        string content,
        DateTime occuredOn,
        string failureReason,
        int retryCount,
        CancellationToken ct);

    Task<IEnumerable<DeadLetterMessage>> GetAllAsync(
        int skip,
        int take,
        CancellationToken ct);

    Task RetryAsync(Guid messageId, CancellationToken ct);
    Task DeleteAsync(Guid messageId, CancellationToken ct);

    Task MarkAsResolvedAsync(
        Guid messageId,
        string resolutionNotes,
        CancellationToken ct);
}
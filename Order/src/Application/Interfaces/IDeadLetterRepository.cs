namespace Application.Interfaces;

public sealed record DeadLetterMessage(
    Guid id,
    string Type,
    string Content,
    DateTime OccurredOn,
    string FailureReason,
    int RetryCount,
    DateTime MovedToDeadLetterAt);

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
}
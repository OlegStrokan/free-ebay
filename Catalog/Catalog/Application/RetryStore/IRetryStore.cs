namespace Application.RetryStore;

public interface IRetryStore
{
    Task PersistAsync(RetryRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<RetryRecord>> GetDueRecordsAsync(int batchSize, CancellationToken ct = default);
    Task MarkInProgressAsync(Guid id, CancellationToken ct = default);
    Task MarkSucceededAsync(Guid id, CancellationToken ct = default);
    Task RescheduleAsync(Guid id, int newRetryCount, DateTime nextRetryAt, string? errorMessage, string? errorType, CancellationToken ct = default);
    Task MarkDeadLetterAsync(Guid id, string? errorMessage, string? errorType, CancellationToken ct = default);
}

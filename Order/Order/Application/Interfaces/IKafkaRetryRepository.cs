using Application.Models;

namespace Application.Interfaces;

public interface IKafkaRetryRepository
{
    Task PersistAsync(KafkaRetryRecord record, CancellationToken ct = default);

    Task<IReadOnlyList<KafkaRetryRecord>> GetDueRecordsAsync(int batchSize, CancellationToken ct = default);

    Task<IReadOnlyList<KafkaRetryRecord>> ClaimDueRecordsAsync(int batchSize, CancellationToken ct = default);

    Task MarkInProgressAsync(Guid id, CancellationToken ct = default);

    Task MarkSucceededAsync(Guid id, CancellationToken ct = default);

    Task RescheduleAsync(
        Guid id,
        int newRetryCount,
        DateTime nextRetryAt,
        string? errorMessage,
        string? errorType,
        CancellationToken ct = default);

    Task MarkDeadLetterAsync(
        Guid id,
        string? errorMessage,
        string? errorType,
        CancellationToken ct = default);
}

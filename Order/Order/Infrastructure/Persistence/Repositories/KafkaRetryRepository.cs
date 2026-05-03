using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class KafkaRetryRepository(
    AppDbContext dbContext,
    ILogger<KafkaRetryRepository> logger) : IKafkaRetryRepository
{
    public async Task PersistAsync(KafkaRetryRecord record, CancellationToken ct = default)
    {
        dbContext.KafkaRetryRecords.Add(record);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Persisted KafkaRetryRecord {RecordId} for event {EventType} (topic={Topic}, partition={Partition}, offset={Offset})",
            record.Id, record.EventType, record.Topic, record.Partition, record.Offset);
    }

    public async Task<IReadOnlyList<KafkaRetryRecord>> GetDueRecordsAsync(
        int batchSize, CancellationToken ct = default)
    {
        return await dbContext.KafkaRetryRecords
            .Where(r => r.Status == KafkaRetryRecordStatus.Pending && r.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(r => r.NextRetryAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<KafkaRetryRecord>> ClaimDueRecordsAsync(
        int batchSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // concurrent replicas never claim the same records.
        var claimedIds = await dbContext.Database
            .SqlQueryRaw<Guid>(
                """
                UPDATE "KafkaRetryRecords"
                SET "Status" = {0}
                WHERE "Id" IN (
                    SELECT "Id" FROM "KafkaRetryRecords"
                    WHERE "Status" = {1} AND "NextRetryAt" <= {2}
                    ORDER BY "NextRetryAt"
                    LIMIT {3}
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING "Id"
                """,
                (int)KafkaRetryRecordStatus.InProgress,
                (int)KafkaRetryRecordStatus.Pending,
                now,
                batchSize)
            .ToListAsync(ct);

        if (claimedIds.Count == 0)
            return [];

        return await dbContext.KafkaRetryRecords
            .Where(r => claimedIds.Contains(r.Id))
            .OrderBy(r => r.NextRetryAt)
            .ToListAsync(ct);
    }

    public async Task MarkInProgressAsync(Guid id, CancellationToken ct = default)
    {
        var record = await GetRequiredAsync(id, ct);
        record.MarkInProgress();
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkSucceededAsync(Guid id, CancellationToken ct = default)
    {
        var record = await GetRequiredAsync(id, ct);
        // @think: is this policically correct to call domain method in persistance service?
        record.MarkSucceeded();
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("KafkaRetryRecord {RecordId} marked Succeeded", id);
    }

    public async Task RescheduleAsync(
        Guid id,
        int newRetryCount,
        DateTime nextRetryAt,
        string? errorMessage,
        string? errorType,
        CancellationToken ct = default)
    {
        var record = await GetRequiredAsync(id, ct);
        record.Reschedule(newRetryCount, nextRetryAt, errorMessage, errorType);
        await dbContext.SaveChangesAsync(ct);

        logger.LogWarning(
            "KafkaRetryRecord {RecordId} rescheduled (attempt={Attempt}, nextRetry={NextRetry})",
            id, newRetryCount, nextRetryAt);
    }

    public async Task MarkDeadLetterAsync(
        Guid id,
        string? errorMessage,
        string? errorType,
        CancellationToken ct = default)
    {
        var record = await GetRequiredAsync(id, ct);
        record.MarkDeadLetter(errorMessage, errorType);
        await dbContext.SaveChangesAsync(ct);

        logger.LogError(
            "KafkaRetryRecord {RecordId} moved to DeadLetter after {Retries} attempts. Reason: {Reason}",
            id, record.RetryCount, errorMessage);
    }

    private async Task<KafkaRetryRecord> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var record = await dbContext.KafkaRetryRecords.FindAsync([id], ct);
        if (record is null)
            throw new InvalidOperationException($"KafkaRetryRecord {id} not found");
        return record;
    }
}

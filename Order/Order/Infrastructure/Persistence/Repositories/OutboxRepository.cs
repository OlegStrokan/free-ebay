using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OutboxRepository(AppDbContext dbContext) : IOutboxRepository
{
    public async Task AddAsync(Guid messageId, string type, string content, DateTime occurredOn, string aggregateId, CancellationToken ct)
    {
        var outboxMessage = new OutboxMessage(messageId, type, content, occurredOn, aggregateId);

        await dbContext.OutboxMessages.AddAsync(outboxMessage, ct);
        
        // saveChanges called by unit of work (we use transaction in handler)
        
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimUnprocessedMessagesAsync(int batchSize, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var staleThreshold = now.AddMinutes(-5);

        // concurrent replicas never claim the same messages
        var claimedIds = await dbContext.Database
            .SqlQueryRaw<Guid>(
                """
                UPDATE "OutboxMessages"
                SET "ClaimedAtUtc" = {0}
                WHERE "Id" IN (
                    SELECT "Id" FROM "OutboxMessages"
                    WHERE "ProcessedOnUtc" IS NULL
                      AND ("ClaimedAtUtc" IS NULL OR "ClaimedAtUtc" < {1})
                    ORDER BY "OccurredOnUtc"
                    LIMIT {2}
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING "Id"
                """,
                now, staleThreshold, batchSize)
            .ToListAsync(ct);

        if (claimedIds.Count == 0)
            return [];

        return await dbContext.OutboxMessages
            .Where(m => claimedIds.Contains(m.Id))
            .OrderBy(m => m.OccurredOnUtc)
            .ToListAsync(ct);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message != null)
        {
            message.MarkAsProcessed(DateTime.UtcNow);
            await dbContext.SaveChangesAsync(ct);
        }
        
    }

    public async Task IncrementRetryCountAsync(Guid messageId, CancellationToken ct)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message != null)
        {
            message.UpdateFailure("Processing failed", DateTime.UtcNow);

            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Guid messageId, CancellationToken ct)
    {
        await dbContext.OutboxMessages
            .Where(m => m.Id == messageId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteProcessedMessagesAsync(DateTime olderThen, CancellationToken ct)
    {
        await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < olderThen)
            .ExecuteDeleteAsync(ct);
    }
}
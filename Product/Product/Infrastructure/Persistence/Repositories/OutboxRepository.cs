using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class OutboxRepository(ProductDbContext dbContext) : IOutboxRepository
{
    public async Task AddAsync(Guid messageId, string type, string content, DateTime occurredOn,
                               string aggregateId, CancellationToken ct = default)
    {
        var message = new OutboxMessage(messageId, type, content, occurredOn, aggregateId);
        await dbContext.OutboxMessages.AddAsync(message, ct);
        // SaveChanges called by ProductPersistenceService inside the transaction
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, int maxRetries, CancellationToken ct = default)
    {
        return await dbContext.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < maxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is not null)
        {
            message.MarkAsProcessed();
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task IncrementRetryCountAsync(Guid messageId, string error, CancellationToken ct = default)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is not null)
        {
            message.IncrementRetry(error);
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Guid messageId, CancellationToken ct = default)
    {
        await dbContext.OutboxMessages
            .Where(m => m.Id == messageId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteProcessedMessagesAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        await dbContext.OutboxMessages
            .Where(m => m.ProcessedOn != null && m.ProcessedOn < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}

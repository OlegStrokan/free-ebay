using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OutboxRepository(AppDbContext dbContext) : IOutboxRepository
{
    public async Task AddAsync(Guid messageId, string type, string content, DateTime occurredOn, CancellationToken ct)
    {
        var outboxMessage = new OutboxMessage(messageId, type, content, occurredOn);

        await dbContext.OutboxMessages.AddAsync(outboxMessage, ct);
        
        // saveChanges called by unit of work (we use transaction in handler)
        
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken ct)
    {
        return await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
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
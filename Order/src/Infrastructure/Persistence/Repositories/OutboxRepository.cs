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

    public Task IncrementRetryCountAsync(Guid messageId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid messageId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteProcessedMessagesAsync(DateTime olderThen, CancellationToken ct)
    {
        var oldMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < olderThen)
            .ToListAsync(ct);

        if (oldMessages.Any())
        {
            dbContext.OutboxMessages.RemoveRange(oldMessages);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
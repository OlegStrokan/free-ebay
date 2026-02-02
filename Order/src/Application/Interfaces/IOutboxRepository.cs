
using Application.Models;

namespace Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(
        Guid messageId,
        string type,
        string content, 
        DateTime occurredOn,
        CancellationToken ct
        );
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken ct);
    
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct);

    Task IncrementRetryCountAsync(Guid messageId, CancellationToken ct);
    
    Task DeleteAsync(Guid messageId, CancellationToken ct);
    
    Task DeleteProcessedMessagesAsync(DateTime olderThen, CancellationToken ct);
}
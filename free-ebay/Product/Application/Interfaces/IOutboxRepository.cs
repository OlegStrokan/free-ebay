using Application.Models;

namespace Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(Guid messageId, string type, string content, DateTime occurredOn,
                  string aggregateId, CancellationToken ct = default);
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken ct = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct = default);
    Task IncrementRetryCountAsync(Guid messageId, string error, CancellationToken ct = default);
    Task DeleteAsync(Guid messageId, CancellationToken ct = default);
    Task DeleteProcessedMessagesAsync(CancellationToken ct = default);
}

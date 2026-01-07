using Domain.Entities;

namespace Application.Interfaces;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken ct);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct);
    Task DeleteProcessedMessagesAsync(DateTime olderThen, CancellationToken ct);
}
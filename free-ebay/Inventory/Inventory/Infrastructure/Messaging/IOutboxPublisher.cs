using Infrastructure.Persistence.Entities;

namespace Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken);
}

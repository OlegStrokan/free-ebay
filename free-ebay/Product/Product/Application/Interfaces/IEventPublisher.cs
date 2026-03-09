using Domain.Common;

namespace Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent;

    Task PublishRawAsync(Guid id, string typeName, string content,
                         DateTime occurredOn, string aggregateId, CancellationToken ct = default);
}

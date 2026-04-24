using Domain.Common;

namespace Application.Interfaces;

public interface IEventPublisher
{
    // for command handler
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent;
    // for outbox processor
    Task PublishRawAsync(Guid id, string typeName, string content, DateTime occuredOn, string aggregateId, CancellationToken cancellationToken);
    // serialize a domain event into the Kafka-ready payload format (DTO mapping, flat types)
    string Serialize(IDomainEvent @event);
}
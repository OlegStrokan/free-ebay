using Domain.Common;

namespace Application.Interfaces;

public interface IEventPublisher
{
    // for command handler
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent;
    // for outbox processor
    Task PublishRawAsync(Guid id, string typeName, string content, DateTime occuredOn,  CancellationToken cancellationToken);
}
using Domain.Common;

namespace Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent;
}
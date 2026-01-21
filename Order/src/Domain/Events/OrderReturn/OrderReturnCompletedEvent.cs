using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public record OrderReturnCompletedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime CompetedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = CompetedAt;
}
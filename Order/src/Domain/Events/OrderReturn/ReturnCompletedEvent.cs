using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public sealed record ReturnCompletedEvent(
    ReturnRequestId ReturnRequestId,
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

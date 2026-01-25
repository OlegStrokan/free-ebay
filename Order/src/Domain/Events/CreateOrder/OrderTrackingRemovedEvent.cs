using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public record OrderTrackingRemovedEvent(
    OrderId OrderId,
    TrackingId RemovedTrackingId,
    DateTime RemovedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = RemovedAt;
}
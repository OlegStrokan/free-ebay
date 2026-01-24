using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public record OrderTrackingAssignedEvent(
    OrderId OrderId,
    TrackingId TrackingId,
    DateTime AssignedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = AssignedAt;
}

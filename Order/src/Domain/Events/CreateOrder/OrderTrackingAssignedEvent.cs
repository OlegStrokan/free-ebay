using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public record OrderTrackingAssignedEvent(
    OrderId OrderId,
    TrackingId TrackingId,
    DateTime AssignedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => AssignedAt;
}
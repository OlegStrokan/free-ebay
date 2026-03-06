using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public record OrderTrackingRemovedEvent(
    OrderId OrderId,
    TrackingId RemovedTrackingId,
    DateTime RemovedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => RemovedAt;
}
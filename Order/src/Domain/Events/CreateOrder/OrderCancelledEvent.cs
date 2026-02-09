using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public sealed record OrderCancelledEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    List<string> Reasons,
    DateTime CancelledAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CancelledAt;
}
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record OrderApprovedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime ApprovedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
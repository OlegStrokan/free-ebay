using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record OrderCancelledEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    string FailureReason,
    DateTime CancelledAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
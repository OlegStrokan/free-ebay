using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record OrderPaidEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    Money Amount,
    DateTime PaidAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
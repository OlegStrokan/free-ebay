using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.B2BOrder;

public sealed record B2BOrderCancelledEvent(
    B2BOrderId B2BOrderId,
    List<string> Reasons,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

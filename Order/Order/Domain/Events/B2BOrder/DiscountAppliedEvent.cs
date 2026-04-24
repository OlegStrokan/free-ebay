using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.B2BOrder;

public sealed record DiscountAppliedEvent(
    B2BOrderId B2BOrderId,
    decimal DiscountPercent,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

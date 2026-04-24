using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.B2BOrder;

public sealed record B2BOrderStartedEvent(
    B2BOrderId B2BOrderId,
    CustomerId CustomerId,
    string CompanyName,
    Address DeliveryAddress,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

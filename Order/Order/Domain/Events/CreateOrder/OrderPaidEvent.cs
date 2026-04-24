using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public sealed record OrderPaidEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    PaymentId PaymentId,
    Money Amount,
    DateTime PaidAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => PaidAt;
}
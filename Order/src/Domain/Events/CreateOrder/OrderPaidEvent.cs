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
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public record OrderRefundedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    string RefundId,
    Money RefundAmount,
    DateTime RefundedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = RefundedAt;
}
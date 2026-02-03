using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public sealed record ReturnRefundProcessedEvent(
    ReturnRequestId ReturnRequestId,
    OrderId OrderId,
    CustomerId CustomerId,
    string RefundId,
    Money RefundAmount,
    DateTime RefundedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
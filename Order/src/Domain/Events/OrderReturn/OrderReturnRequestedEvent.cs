using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public record OrderReturnRequestedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    string Reason,
    List<OrderItem> ItemToReturn,
    Money RefundAmount,
    DateTime RequestedAt
    ) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = RequestedAt;
}
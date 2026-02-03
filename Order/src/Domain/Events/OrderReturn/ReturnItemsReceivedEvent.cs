using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public sealed record ReturnItemsReceivedEvent(
    ReturnRequestId ReturnRequestId,
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime ReceivedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}


using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public record OrderReturnReceivedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime ReceivedAt
 
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = ReceivedAt;
}
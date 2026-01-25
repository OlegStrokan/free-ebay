using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events.CreateOrder;

public record OrderReturnReceiptRevertedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTime RevertedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = RevertedAt;
}
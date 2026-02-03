using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Events.OrderReturn;

public sealed record ReturnRequestCreatedEvent(
    ReturnRequestId ReturnRequestId,
    OrderId OrderId,
    CustomerId CustomerId,
    string Reason,
    List<OrderItem> ItemsToReturn,
    Money RefundAmount,
    DateTime RequestedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
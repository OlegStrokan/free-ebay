using Domain.Common;
using Domain.Entities.Subscription;
using Domain.ValueObjects;

namespace Domain.Events.Subscription;

public sealed record RecurringOrderCreatedEvent(
    RecurringOrderId RecurringOrderId,
    CustomerId CustomerId,
    string Frequency,
    List<RecurringOrderItem> Items,
    Address DeliveryAddress,
    string PaymentMethod,
    DateTime NextRunAt,
    int? MaxExecutions,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CreatedAt;
}

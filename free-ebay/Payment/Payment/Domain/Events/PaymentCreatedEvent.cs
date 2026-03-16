using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record PaymentCreatedEvent(
    PaymentId PaymentId,
    string OrderId,
    string CustomerId,
    Money Amount,
    PaymentMethod PaymentMethod,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn => CreatedAt;
}
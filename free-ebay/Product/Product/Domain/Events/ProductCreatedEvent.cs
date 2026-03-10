using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ProductCreatedEvent(
    ProductId ProductId,
    SellerId SellerId,
    string Name,
    string Description,
    CategoryId CategoryId,
    Money Price,
    int InitialStock,
    List<ProductAttribute> Attributes,
    List<string> ImageUrls,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => CreatedAt;
}

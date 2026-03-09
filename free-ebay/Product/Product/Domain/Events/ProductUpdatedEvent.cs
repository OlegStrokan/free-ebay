using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Events;

public sealed record ProductUpdatedEvent(
    ProductId              ProductId,
    string                 Name,
    string                 Description,
    CategoryId             CategoryId,
    Money                  Price,
    List<ProductAttribute> Attributes,
    List<string>           ImageUrls,
    DateTime               UpdatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn => UpdatedAt;
}

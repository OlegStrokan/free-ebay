using Domain.Common;
using Domain.Entities;

namespace Domain.Tests.Entities;

public class DomainEventTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenAllParametersAreValid()
    {
        var evt = DomainEvent.Create(
            aggregateId: "order-abc",
            aggregateType: AggregateTypes.Order,
            eventType: "OrderCreatedEvent",
            eventData: "{\"total\":100}",
            version: 1,
            eventId: Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal("order-abc", evt.AggregateId);
        Assert.Equal(AggregateTypes.Order, evt.AggregateType);
        Assert.Equal("OrderCreatedEvent", evt.EventType);
        Assert.Equal("{\"total\":100}", evt.EventData);
        Assert.Equal(1, evt.Version);
    }

    [Fact]
    public void Create_ShouldSetOccurredOn_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow;

        var evt = DomainEvent.Create("id", "Type", "EventType", "{}", 0, Guid.NewGuid());

        Assert.True(evt.OccurredOn >= before);
        Assert.True(evt.OccurredOn <= DateTime.UtcNow);
    }

    [Fact]
    public void OccurredOn_ShouldMatchIDomainEventInterface()
    {
        var evt = DomainEvent.Create("id", "Type", "EventType", "{}", 0, Guid.NewGuid());

        var iface = (Domain.Common.IDomainEvent)evt;
        Assert.Equal(evt.OccurredOn, iface.OccurredOn);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueEventIds()
    {
        var e1 = DomainEvent.Create("id", "Type", "Event", "{}", 0, Guid.NewGuid());
        var e2 = DomainEvent.Create("id", "Type", "Event", "{}", 0, Guid.NewGuid());

        Assert.NotEqual(e1.EventId, e2.EventId);
    }

    [Fact]
    public void Create_ShouldAllowVersionZero()
    {
        var evt = DomainEvent.Create("id", "Type", "EventType", "{}", 0, Guid.NewGuid());

        Assert.Equal(0, evt.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenAggregateIdIsNullOrWhitespace(string? aggregateId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainEvent.Create(aggregateId!, AggregateTypes.Order, "EventType", "{}", 0, Guid.NewGuid()));

        Assert.Contains("AggregateId is required", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenAggregateTypeIsNullOrWhitespace(string? aggregateType)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainEvent.Create("id", aggregateType!, "EventType", "{}", 0, Guid.NewGuid()));

        Assert.Contains("AggregateType is required", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenEventTypeIsNullOrWhitespace(string? eventType)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainEvent.Create("id", AggregateTypes.Order, eventType!, "{}", 0, Guid.NewGuid()));

        Assert.Contains("EventType is required", ex.Message);
    }
}

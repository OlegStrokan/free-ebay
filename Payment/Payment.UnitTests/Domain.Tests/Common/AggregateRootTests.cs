using Domain.Common;

namespace Domain.Tests.Common;

public class AggregateRootTests
{
    private sealed class TestEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate() : base(Guid.NewGuid()) { }
        public void Raise(IDomainEvent evt) => AddDomainEvent(evt);
    }

    [Fact]
    public void DomainEvents_ShouldBeEmptyOnCreate()
    {
        var aggregate = new TestAggregate();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_ShouldAppendEventToList()
    {
        var aggregate = new TestAggregate();
        var evt = new TestEvent();

        aggregate.Raise(evt);

        Assert.Single(aggregate.DomainEvents);
        Assert.Same(evt, aggregate.DomainEvents[0]);
    }

    [Fact]
    public void AddDomainEvent_MultipleTimes_ShouldPreserveOrder()
    {
        var aggregate = new TestAggregate();
        var first = new TestEvent();
        var second = new TestEvent();

        aggregate.Raise(first);
        aggregate.Raise(second);

        Assert.Equal(2, aggregate.DomainEvents.Count);
        Assert.Same(first, aggregate.DomainEvents[0]);
        Assert.Same(second, aggregate.DomainEvents[1]);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.Raise(new TestEvent());
        aggregate.Raise(new TestEvent());

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainEvents_ShouldReturnReadOnlyList()
    {
        var aggregate = new TestAggregate();

        Assert.IsAssignableFrom<IReadOnlyList<IDomainEvent>>(aggregate.DomainEvents);
    }
}

public class EntityEqualityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void Equals_TwoEntitiesWithSameId_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_TwoEntitiesWithDifferentId_ShouldNotBeEqual()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_SameReference_ShouldBeEqual()
    {
        var a = new TestEntity(Guid.NewGuid());

        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var a = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void OperatorEquals_TwoEntitiesWithSameId_ShouldBeTrue()
    {
        var id = Guid.NewGuid();
        Entity<Guid> a = new TestEntity(id);
        Entity<Guid> b = new TestEntity(id);

        Assert.True(a == b);
    }

    [Fact]
    public void OperatorNotEquals_TwoEntitiesWithDifferentId_ShouldBeTrue()
    {
        Entity<Guid> a = new TestEntity(Guid.NewGuid());
        Entity<Guid> b = new TestEntity(Guid.NewGuid());

        Assert.True(a != b);
    }

    [Fact]
    public void OperatorEquals_BothNull_ShouldBeTrue()
    {
        Entity<Guid>? a = null;
        Entity<Guid>? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void OperatorEquals_OneNull_ShouldBeFalse()
    {
        Entity<Guid>? a = new TestEntity(Guid.NewGuid());
        Entity<Guid>? b = null;

        Assert.False(a == b);
    }

    [Fact]
    public void GetHashCode_SameId_ShouldReturnSameHash()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}

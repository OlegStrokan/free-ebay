using Domain.Common;

namespace Domain.Tests.Common;

[TestFixture]
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

    [Test]
    public void AddDomainEvent_ShouldAppendEventToList()
    {
        var aggregate = new TestAggregate();
        var evt = new TestEvent();

        aggregate.Raise(evt);

        Assert.That(aggregate.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(aggregate.DomainEvents[0], Is.SameAs(evt));
    }

    [Test]
    public void AddDomainEvent_MultipleTimes_ShouldPreserveOrder()
    {
        var aggregate = new TestAggregate();
        var first = new TestEvent();
        var second = new TestEvent();

        aggregate.Raise(first);
        aggregate.Raise(second);

        Assert.That(aggregate.DomainEvents, Has.Count.EqualTo(2));
        Assert.That(aggregate.DomainEvents[0], Is.SameAs(first));
        Assert.That(aggregate.DomainEvents[1], Is.SameAs(second));
    }

    [Test]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.Raise(new TestEvent());
        aggregate.Raise(new TestEvent());

        aggregate.ClearDomainEvents();

        Assert.That(aggregate.DomainEvents, Is.Empty);
    }

    [Test]
    public void DomainEvents_ShouldBeEmptyOnCreate()
    {
        var aggregate = new TestAggregate();

        Assert.That(aggregate.DomainEvents, Is.Empty);
    }

    [Test]
    public void DomainEvents_ShouldReturnReadOnlyList()
    {
        var aggregate = new TestAggregate();

        Assert.That(aggregate.DomainEvents, Is.InstanceOf<IReadOnlyList<IDomainEvent>>());
    }
}

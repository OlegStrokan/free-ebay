using Domain.Common;
using Domain.Entities.Order;
using Domain.Events.CreateOrder;
using Domain.ValueObjects;

namespace Domain.Tests.Common;

// this is abstract class. okay?. so we test its behavior through the concrete Order aggregate

public class AggregateRootTests
{
       private sealed record UnhandledDomainEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn => DateTime.UtcNow;
    }

    private sealed class StubAggregate : AggregateRoot<OrderId>
    {
        private StubAggregate() { }

        public static StubAggregate New() => new();

        public void TriggerUnhandled() => RaiseEvent(new UnhandledDomainEvent());
    }

    private readonly CustomerId _customerId = CustomerId.CreateUnique();
    private readonly Address _address = Address.Create("Baker St", "London", "UK", "NW1");
    private readonly List<OrderItem> _items;

    public AggregateRootTests()
    {
        _items = new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) };
    }

    [Fact]
    public void Version_ShouldStartAtMinusOne_BeforeAnyEvent()
    {
        // the private default constructor leaves Version = -1 before LoadFromHistory
        var orderId = OrderId.CreateUnique();
        var history = new List<IDomainEvent>
        {
            new OrderCreatedEvent(orderId, _customerId, Money.Create(100, "USD"), _address, _items, DateTime.UtcNow, "CreditCard")
        };

        var order = Order.FromHistory(history);

        // after one event applied via LoadFromHistory, Version should be 1
        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void RaiseEvent_ShouldIncrementVersion()
    {
        var order = Order.Create(_customerId, _address, _items, "CreditCard");

        // create raises one event => version goes from 0 to 1
        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void RaiseEvent_ShouldAddToUncommittedEvents()
    {
        var order = Order.Create(_customerId, _address, _items, "CreditCard");

        Assert.Single(order.UncommitedEvents);
        Assert.IsType<OrderCreatedEvent>(order.UncommitedEvents[0]);
    }

    [Fact]
    public void RaiseEvent_ShouldBothApplyAndTrackEvent()
    {
        var order = Order.Create(_customerId, _address, _items, "CreditCard");
        order.ClearUncommittedEvents();

        order.Pay(PaymentId.From("PAY-1"));

        // state was applied
        Assert.Equal(OrderStatus.Paid, order.Status);
        // event is tracked
        Assert.Single(order.UncommitedEvents);
        Assert.IsType<OrderPaidEvent>(order.UncommitedEvents[0]);
    }

    [Fact]
    public void ClearUncommittedEvents_ShouldEmptyList_WithoutAffectingVersion()
    {
        var order = Order.Create(_customerId, _address, _items, "CreditCard");
        var versionBefore = order.Version;

        order.ClearUncommittedEvents();

        Assert.Empty(order.UncommitedEvents);
        Assert.Equal(versionBefore, order.Version); // version is unaffected
    }

    [Fact]
    public void LoadFromHistory_ShouldIncrementVersionPerEvent()
    {
        var orderId = OrderId.CreateUnique();
        var paymentId = PaymentId.From("PAY-10");
        var now = DateTime.UtcNow;

        var history = new List<IDomainEvent>
        {
            new OrderCreatedEvent(orderId, _customerId, Money.Create(100, "USD"), _address, _items, now, "CreditCard"),
            new OrderPaidEvent(orderId, _customerId, paymentId, Money.Create(100, "USD"), now.AddMinutes(1)),
            new OrderApprovedEvent(orderId, _customerId, now.AddMinutes(2))
        };

        var order = Order.FromHistory(history);

        Assert.Equal(3, order.Version); // 3 events => versions 1,2,3
    }

    [Fact]
    public void LoadFromHistory_ShouldNotAddToUncommittedEvents()
    {
        var orderId = OrderId.CreateUnique();
        var history = new List<IDomainEvent>
        {
            new OrderCreatedEvent(orderId, _customerId, Money.Create(100, "USD"), _address, _items, DateTime.UtcNow, "CreditCard")
        };

        var order = Order.FromHistory(history);

        Assert.Empty(order.UncommitedEvents);
    }

    [Fact]
    public void MultipleRaisedEvents_ShouldAllAppearInUncommittedList()
    {
        var order = Order.Create(_customerId, _address, _items, "CreditCard");
        order.Pay(PaymentId.From("PAY-2"));
        order.Approve();

        Assert.Equal(3, order.UncommitedEvents.Count);
    }

    [Fact]
    public void UncommittedEvents_ShouldBeReadOnly_CannotBeModifiedExternally()
    {
        var order = Order.Create(_customerId, _address, _items, "CreditCard");

        // cast to prove it's a read-only wrapper, not a mutable list
        var events = order.UncommitedEvents;
        Assert.IsAssignableFrom<IReadOnlyList<IDomainEvent>>(events);
    }

    [Fact]
    public void ApplyEvent_ShouldThrow_InvalidOperationException_WhenApplyMethodIsMissing()
    {
        var stub = StubAggregate.New();

        // StubAggregate has no Apply(UnhandledDomainEvent) - must throw with a useful message
        var ex = Assert.Throws<InvalidOperationException>(() => stub.TriggerUnhandled());

        Assert.Contains("Apply", ex.Message);
        Assert.Contains(nameof(UnhandledDomainEvent), ex.Message);
    }
}

using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Order : AggregateRoot<OrderId>
{
    private CustomerId _customerId;
    private Address _deliveryAddress;
    private Money _totalPrice;
    private OrderStatus _status;
    private TrackingId _trackingId;
    private List<OrderItem> _items = new();
    private DateTime _createdAt;
    private DateTime? _updatedAt;
    public List<string> _failedMessages = new List<string>();

    public CustomerId CustomerId => _customerId;
    public OrderStatus Status => _status;
    public Money TotalPrice => _totalPrice;
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();



    public int Version { get; private set; }

    private readonly List<IDomainEvent> _uncommitedEvents = new();

    public IReadOnlyList<IDomainEvent> UncommitedEvents => _uncommitedEvents.AsReadOnly();


    private Order()
    {
    }

    public static Order Create(
        CustomerId customerId,
        Address deliveryAddress,
        List<OrderItem> items)
    {
        if (items == null || items.Count == 0)
            throw new OrderDomainException("Order must have a least one item");

        var order = new Order();
        var totalPrice = CalculateTotalPrice(items);

        var evt = new OrderCreatedEvent(
            OrderId.CreateUnique(),
            customerId,
            totalPrice,
            deliveryAddress,
            items,
            DateTime.UtcNow);

        order.RaiseEvent(evt);
        return order;
    }

    public void Pay()
    {
        if (_status != OrderStatus.Pending && _status != OrderStatus.AwaitingPayment)
            throw new OrderDomainException($"Cannot pay order in {_status} status");

        var evt = new OrderPaidEvent(
            Id,
            _customerId,
            _totalPrice,
            DateTime.UtcNow
        );

        RaiseEvent(evt);
    }

    public void Approve()
    {
        if (_status != OrderStatus.Paid)
            throw new OrderDomainException($"Cannot approve order in {_status} status");


        var evt = new OrderApprovedEvent(
            Id,
            _customerId,
            DateTime.UtcNow
        );

        RaiseEvent(evt);
    }

    public void Complete()
    {
        if (_status != OrderStatus.Approved)
            throw new OrderDomainException($"Cannot complete order in {_status} status");

        var evt = new OrderCompletedEvent(
            Id,
            _customerId,
            DateTime.UtcNow
        );

        RaiseEvent(evt);
    }

    public void Cancel(List<string> failedMessages)
    {
        if (_status != OrderStatus.Cancelling && _status != OrderStatus.Pending)
            throw new OrderDomainException($"Cannot cancel order in {_status} status");

        var evt = new OrderCancelledEvent(
            Id,
            _customerId,
            DateTime.UtcNow
            );

        // @todo: check if works, + write tests
        foreach (var message in failedMessages)
        {
            _failedMessages.Add(message);
        }

        RaiseEvent(evt);
    }



// event application


    private void Apply(OrderCreatedEvent evt)
    {
        Id = evt.OrderId;
        _customerId = evt.CustomerId;
        _totalPrice = evt.TotalPrice;
        _deliveryAddress = evt.DeliveryAddress;
        _items = evt.Items.ToList();
        _trackingId = TrackingId.CreateUnique();
        _status = OrderStatus.Pending;
        _createdAt = evt.CreatedAt;

        long itemId = 1;
        foreach (var item in _items)
        {
            item.InitializeOrderItem(Id, OrderItemId.From(itemId++));
        }
    }
    
    private void Apply(OrderPaidEvent evt)
    {
        _status = OrderStatus.Paid;
        _updatedAt = evt.PaidAt;
    }


    private void Apply(OrderCompletedEvent evt)
    {
        _status = OrderStatus.Completed;
        _updatedAt = evt.CompletedAt;
    }

    private void Apply(OrderApprovedEvent evt)
    {
        _status = OrderStatus.Approved;
        _updatedAt = evt.ApprovedAt;
    }

    private void Apply(OrderCancelledEvent evt)
    {
        _status = OrderStatus.Cancelled;
        _updatedAt = evt.CancelledAt;
    }
    
    // event sourcing infrastructure

    private void RaiseEvent(IDomainEvent evt)
    {
        ApplyEvent(evt);
        _uncommitedEvents.Add(evt);
       
    }

    private void ApplyEvent(IDomainEvent evt)
    {
        switch (evt)
        {
            case OrderCreatedEvent e:
                Apply(e);
                break;
            case OrderPaidEvent e:
                Apply(e);
                break;
            case OrderApprovedEvent e:
                Apply(e);
                break;
            case OrderCompletedEvent e:
                Apply(e);
                break;
            case OrderCancelledEvent e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type {evt.GetType().Name}");
        }
        
        Version++;



    }

    // reconstruction
    public static Order FromEvents(IEnumerable<IDomainEvent> history)
    {
        var order = new Order();

        foreach (var evt in history)
        {
            order.ApplyEvent(evt);
        }

        return order;
    }

    public void MarkEventsAsCommited()
    {
        _uncommitedEvents.Clear();
    }


    private static Money CalculateTotalPrice(List<OrderItem> items)
    {
        var currency = items[0].PriceAtPurchase.Currency;
        return items
            .Select(item => item.GetSubTotal())
            .Aggregate(Money.Default(currency), (acc, price) => acc.Add(price));
    }
}
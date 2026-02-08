using System.Diagnostics.Tracing;
using Domain.Common;
using Domain.Events.CreateOrder;
using Domain.Events.OrderReturn;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Order : AggregateRoot<OrderId>
{
    private CustomerId _customerId;
    private Address _deliveryAddress;
    private Money _totalPrice;
    private OrderStatus _status;
    private TrackingId? _trackingId;
    private PaymentId? _paymentId;
    
    private List<OrderItem> _items = new();
    private DateTime _createdAt;
    private DateTime? _completedAt;
    private DateTime? _updatedAt;
    private List<string> _failedMessages = new List<string>();
    
    public CustomerId CustomerId => _customerId;
    public TrackingId? TrackingId => _trackingId;
    public PaymentId? PaymentId => _paymentId;
    public DateTime? CompletedAt => _completedAt;
    public OrderStatus Status => _status;
    public Money TotalPrice => _totalPrice;
    
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyList<string> FailedMessage => _failedMessages.AsReadOnly();

    public int Version { get; private set; }

    private readonly List<IDomainEvent> _uncommitedEvents = new();

    public IReadOnlyList<IDomainEvent> UncommitedEvents => _uncommitedEvents.AsReadOnly();


    private Order() { }

    public static Order Create(
        CustomerId customerId,
        Address deliveryAddress,
        List<OrderItem> items)
    {
        if (items == null || items.Count == 0)
            throw new DomainException("Order must have a least one item");

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

    public void Pay(PaymentId paymentId)
    {
        _status.ValidateTransitionTo(OrderStatus.Paid);
        
        if (paymentId == null)
            throw new DomainException("Payment ID is required");

        
        var evt = new OrderPaidEvent(
            Id,
            _customerId,
            paymentId,
            _totalPrice,
            DateTime.UtcNow
        );

        RaiseEvent(evt);
    }

    public void Approve()
    {
        _status.ValidateTransitionTo(OrderStatus.Approved);


        var evt = new OrderApprovedEvent(
            Id,
            _customerId,
            DateTime.UtcNow
        );

        RaiseEvent(evt);
    }

    public void Complete()
    {
        _status.ValidateTransitionTo(OrderStatus.Completed);

        var evt = new OrderCompletedEvent(
            Id,
            _customerId,
            DateTime.UtcNow
        );

        RaiseEvent(evt);
    }

    public void Cancel(List<string> failedMessages)
    {
        _status.ValidateTransitionTo(OrderStatus.Cancelled);

        var evt = new OrderCancelledEvent(
            Id,
            _customerId,
            DateTime.UtcNow
            );

        foreach (var message in failedMessages)
        {
            _failedMessages.Add(message);
        }

        RaiseEvent(evt);
    }

    public void AssignTracking(TrackingId trackingId)
    {
        if (!_status.CanAssignTracking())
            throw new DomainException("Cannot assign tracking before payment");

        if (trackingId == null)
            throw new DomainException("Tracking ID is required");

        var evt = new OrderTrackingAssignedEvent(
            Id,
            trackingId,
            DateTime.UtcNow
        );
        
        _trackingId = trackingId;
        _updatedAt = DateTime.UtcNow;
        
        RaiseEvent(evt);
    }

    public void RevertTrackingAssignment()
    {
        if (_trackingId == null)
        {
            return;
        }

        if (!_status.CanAssignTracking())
            throw new DomainException(
                $"Cannot revert tracking for order in {_status} status");

        var evt = new OrderTrackingRemovedEvent(
            Id,
            _trackingId,
            DateTime.UtcNow);
        
        RaiseEvent(evt);
    }

    public bool isEligibleForReturn()
    {
        return Status == OrderStatus.Completed &&
               CompletedAt.HasValue &&
               (DateTime.UtcNow - CompletedAt.Value).TotalDays <= 14;
    }
    
    // event application


    private void Apply(OrderCreatedEvent evt)
    {
        Id = evt.OrderId;
        _customerId = evt.CustomerId;
        _totalPrice = evt.TotalPrice;
        _deliveryAddress = evt.DeliveryAddress;
        _items = evt.Items.ToList();
        _trackingId = null;
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
        _paymentId = evt.PaymentId;
    }


    private void Apply(OrderCompletedEvent evt)
    {
        _status = OrderStatus.Completed;
        _updatedAt = evt.CompletedAt;
        _completedAt = evt.CompletedAt; 
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

    private void Apply(OrderTrackingAssignedEvent evt)
    {
        _trackingId = evt.TrackingId;
        _updatedAt = evt.AssignedAt;
    }

    private void Apply(OrderTrackingRemovedEvent evt)
    {
        _trackingId = null;
        _updatedAt = evt.RemovedAt;
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
            case OrderTrackingAssignedEvent e:
                Apply(e);
                break;
            case OrderTrackingRemovedEvent e:
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


    public static Money CalculateTotalPrice(List<OrderItem> items)
    {
        var currency = items[0].PriceAtPurchase.Currency;
        return items
            .Select(item => item.GetSubTotal())
            .Aggregate(Money.Default(currency), (acc, price) => acc.Add(price));
    }
}
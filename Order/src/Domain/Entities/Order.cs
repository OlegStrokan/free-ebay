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
    private TrackingId _trackingId;
    private List<OrderItem> _items = new();
    private DateTime _createdAt;
    private DateTime? _updatedAt;
    private List<string> _failedMessages = new List<string>();

    private DateTime? _returnRequestedAt;
    private DateTime? _returnReceivedAt;
    private string? _returnReason;
    private List<OrderItem> _returnedItems = new();

    public CustomerId CustomerId => _customerId;
    public OrderStatus Status => _status;
    public Money TotalPrice => _totalPrice;
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyList<string> FailedMessage => _failedMessages.AsReadOnly();
    public IReadOnlyList<OrderItem> ReturnedItems => _returnedItems.AsReadOnly();
    public DateTime? ReturnRequestedAt => _returnRequestedAt;
    public string? ReturnReason => _returnReason;

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

    public void RequestReturn(string reason, List<OrderItem> itemsToReturn)
    {
        if (_status != OrderStatus.Completed)
            throw new OrderDomainException(
                $"Cannot request return for order in {_status} status. Order must be Completed.");

        if (itemsToReturn.Count == 0)
            throw new OrderDomainException("Must specify at least one item to return");

        // validate all items belong to this order
        foreach (var item in itemsToReturn.Where(item => _items.All(i => i.ProductId != item.ProductId)))
        {
            throw new OrderDomainException($"Product {item.ProductId.Value} is not part of this order");
        }

        var refundAmount = CalculateTotalPrice(itemsToReturn);

        var evt = new OrderReturnRequestedEvent(
            Id,
            _customerId,
            reason,
            itemsToReturn,
            refundAmount,
            DateTime.UtcNow);
        
        RaiseEvent(evt);
    }

    public void ConfirmReturnReceived()
    {
        if (_status != OrderStatus.ReturnReceived)
            throw new OrderDomainException($"Cannot confirm return for order in {_status} status");

        var evt = new OrderReturnReceivedEvent(
            Id,
            _customerId,
            DateTime.UtcNow);

        RaiseEvent(evt);
    }

    public void ProcessRefund(string refundId, Money refundAmount)
    {
        if (_status != OrderStatus.ReturnRequested)
            throw new OrderDomainException($"Cannot process refund for order in {_status} status");

        var evt = new OrderRefundedEvent(
            Id,
            _customerId,
            refundId,
            refundAmount,
            DateTime.UtcNow
        );
        
        RaiseEvent(evt);
    }

    public void CompleteReturn()
    {
        if (_status != OrderStatus.Refunded)
            throw new OrderDomainException($"Cannot complete return for order in {_status} status");

        var evt = new OrderReturnCompletedEvent(
            Id,
            _customerId,
            DateTime.UtcNow
        );
        
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

    private void Apply(OrderReturnRequestedEvent evt)
    {
        _status = OrderStatus.ReturnRequested;
        _returnRequestedAt = evt.RequestedAt;
        _returnReason = evt.Reason;
        _returnedItems = evt.ItemToReturn.ToList();
        _updatedAt = evt.RequestedAt;
    }

    private void Apply(OrderReturnReceivedEvent evt)
    {
        _status = OrderStatus.ReturnReceived;
        _returnReceivedAt = evt.ReceivedAt;
        _updatedAt = evt.ReceivedAt;
    }

    private void Apply(OrderRefundedEvent evt)
    {
        _status = OrderStatus.Refunded;
        _updatedAt = evt.RefundedAt;
    }

    private void Apply(OrderReturnCompletedEvent evt)
    {
        _status = OrderStatus.Returned;
        _updatedAt = evt.CompetedAt;
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
            case OrderReturnRequestedEvent e:
                Apply(e);
                break;
            case OrderReturnReceivedEvent e:
                Apply(e);
                break;
            case OrderRefundedEvent e:
                Apply(e);
                break;
            case OrderReturnCompletedEvent e:
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
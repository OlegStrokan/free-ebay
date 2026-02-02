using Domain.Common;
using Domain.Events.OrderReturn;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class ReturnRequest : AggregateRoot<ReturnRequestId>
{
    private OrderId _orderId;
    private CustomerId _customerId;
    private ReturnStatus _status;
    private string _reason;
    private Money _refundAmount;
    private List<OrderItem> _itemsToReturn = new();
    private DateTime _requestedAt;
    private DateTime? _receivedAt;
    private DateTime? _refundedAt;
    private DateTime? _completedAt;
    private string? _refundId;

    public OrderId OrderId => _orderId;
    public CustomerId CustomerId => _customerId;
    public ReturnStatus Status => _status;
    public string Reason => _reason;
    public Money RefundAmount => _refundAmount;
    public IReadOnlyList<OrderItem> ItemsToReturn => _itemsToReturn.AsReadOnly();
    public DateTime RequestedAt => _requestedAt;
    public DateTime? ReceivedAt => _receivedAt;
    public string? RefundId => _refundId;
    public int Version { get; private set; }

    private readonly List<IDomainEvent> _uncommitedEvents = new();
    public IReadOnlyList<IDomainEvent> UncommitedEvents => _uncommitedEvents.AsReadOnly();

    private ReturnRequest() { }
    
    public static ReturnRequest Create(
        OrderId orderId,
        CustomerId customerId,
        string reason,
        List<OrderItem> itemsToReturn,
        Money refundAmount,
        DateTime orderCompletedAt,
        List<OrderItem> orderItems,
        TimeSpan returnWindow)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new OrderDomainException("Return reason is required");

        if (itemsToReturn == null || itemsToReturn.Count == 0)
            throw new OrderDomainException("Must specify at least one item to return");

        if (!refundAmount.IsGreaterThenZero())
            throw new OrderDomainException("Refund amount must be greater than zero");

        // Validate return window
        var orderAge = DateTime.Now - orderCompletedAt;
        if (orderAge > returnWindow)
            throw new OrderDomainException(
                $"Return window expired. Order was completed {orderAge.Days} days ago. "
                + $"Return window is {returnWindow.Days} days. ");

        // Validate all items belong to the order
        foreach (var item in itemsToReturn.Where(item =>
                     orderItems.All(i => i.ProductId != item.ProductId)))
        {
            throw new OrderDomainException($"Product {item.ProductId.Value} is not part of the order");
        }

        var returnRequest = new ReturnRequest();
        var evt = new ReturnRequestCreatedEvent(
            ReturnRequestId.CreateUnique(),
            orderId,
            customerId,
            reason,
            itemsToReturn,
            refundAmount,
            DateTime.UtcNow);

        returnRequest.RaiseEvent(evt);
        return returnRequest;
    }

    public void MarkAsReceived()
    {
        if (_status != ReturnStatus.Pending)
            throw new OrderDomainException($"Cannot mark as received in {_status} status");

        var evt = new ReturnItemsReceivedEvent(
            Id,
            _orderId,
            _customerId,
            DateTime.UtcNow);

        RaiseEvent(evt);
    }

    public void ProcessRefund(string refundId)
    {
        if (_status != ReturnStatus.Received)
            throw new OrderDomainException($"Cannot process refund in {_status} status");

        if (string.IsNullOrWhiteSpace(refundId))
            throw new OrderDomainException("Refund ID is required");

        var evt = new ReturnRefundProcessedEvent(
            Id,
            _orderId,
            _customerId,
            refundId,
            _refundAmount,
            DateTime.UtcNow);

        RaiseEvent(evt);
    }

    public void Complete()
    {
        if (_status != ReturnStatus.Refunded)
            throw new OrderDomainException($"Cannot complete return in {_status} status");

        var evt = new ReturnCompletedEvent(
            Id,
            _orderId,
            _customerId,
            DateTime.UtcNow);

        RaiseEvent(evt);
    }

    // Event application methods
    private void Apply(ReturnRequestCreatedEvent evt)
    {
        Id = evt.ReturnRequestId;
        _orderId = evt.OrderId;
        _customerId = evt.CustomerId;
        _reason = evt.Reason;
        _itemsToReturn = evt.ItemsToReturn.ToList();
        _refundAmount = evt.RefundAmount;
        _requestedAt = evt.RequestedAt;
        _status = ReturnStatus.Pending;
    }

    private void Apply(ReturnItemsReceivedEvent evt)
    {
        _status = ReturnStatus.Received;
        _receivedAt = evt.ReceivedAt;
    }

    private void Apply(ReturnRefundProcessedEvent evt)
    {
        _status = ReturnStatus.Refunded;
        _refundId = evt.RefundId;
        _refundedAt = evt.RefundedAt;
    }

    private void Apply(ReturnCompletedEvent evt)
    {
        _status = ReturnStatus.Completed;
        _completedAt = evt.CompletedAt;
    }

    // Event sourcing infrastructure
    private void RaiseEvent(IDomainEvent evt)
    {
        ApplyEvent(evt);
        _uncommitedEvents.Add(evt);
    }

    private void ApplyEvent(IDomainEvent evt)
    {
        switch (evt)
        {
            case ReturnRequestCreatedEvent e:
                Apply(e);
                break;
            case ReturnItemsReceivedEvent e:
                Apply(e);
                break;
            case ReturnRefundProcessedEvent e:
                Apply(e);
                break;
            case ReturnCompletedEvent e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type {evt.GetType().Name}");
        }

        Version++;
    }

    public static ReturnRequest FromEvents(IEnumerable<IDomainEvent> history)
    {
        var returnRequest = new ReturnRequest();
        foreach (var evt in history)
        {
            returnRequest.ApplyEvent(evt);
        }
        return returnRequest;
    }

    public void MarkEventsAsCommited()
    {
        _uncommitedEvents.Clear();
    }
}


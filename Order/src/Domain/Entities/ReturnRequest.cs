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
        // validation logic (the gatekeeper)
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Return reason is required");

        if (itemsToReturn == null || !itemsToReturn.Any())
            throw new DomainException("Must specify at least one item to return");

        if (!refundAmount.IsGreaterThenZero())
            throw new DomainException("Refund amount must be greater than zero");

        var orderAge = DateTime.UtcNow - orderCompletedAt;
        if (orderAge > returnWindow)
            throw new DomainException($"Return window expired. Return window is {returnWindow.Days} days.");

        foreach (var item in itemsToReturn)
        {
            if (orderItems.All(i => i.ProductId != item.ProductId))
                throw new DomainException($"Product {item.ProductId.Value} is not part of the order");
        }

        // raise event (the command phase)
        var returnRequest = new ReturnRequest();
        returnRequest.RaiseEvent(new ReturnRequestCreatedEvent(
            ReturnRequestId.CreateUnique(),
            orderId,
            customerId,
            reason,
            itemsToReturn,
            refundAmount,
            DateTime.UtcNow));

        return returnRequest;
    }

    public void MarkAsReceived()
    {
        if (_status != ReturnStatus.Pending)
            throw new DomainException($"Cannot mark as received in {_status} status");

        RaiseEvent(new ReturnItemsReceivedEvent(
            Id,
            _orderId,
            _customerId,
            DateTime.UtcNow));
    }

    public void ProcessRefund(string refundId)
    {
        if (_status != ReturnStatus.Received)
            throw new DomainException($"Cannot process refund in {_status} status");

        if (string.IsNullOrWhiteSpace(refundId))
            throw new DomainException("Refund ID is required");

        RaiseEvent(new ReturnRefundProcessedEvent(
            Id,
            _orderId,
            _customerId,
            refundId,
            _refundAmount,
            DateTime.UtcNow));
    }

    public void Complete()
    {
        if (_status != ReturnStatus.Refunded)
            throw new DomainException($"Cannot complete return in {_status} status");

        RaiseEvent(new ReturnCompletedEvent(
            Id,
            _orderId,
            _customerId,
            DateTime.UtcNow));
    }

    // the assistant - state mutation only ---

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

    // --- rebuild ---

    public static ReturnRequest FromHistory(IEnumerable<IDomainEvent> history)
    {
        var request = new ReturnRequest();
        request.LoadFromHistory(history);
        return request;
    }
}
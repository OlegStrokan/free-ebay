using Domain.Common;
using Domain.Entities.Order;
using Domain.Entities.RequestReturn;
using Domain.Events.OrderReturn;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class RequestReturnTests
{
    private readonly OrderId _orderId = OrderId.CreateUnique();
    private readonly CustomerId _customerId = CustomerId.CreateUnique();
    private readonly Money _refundAmount = Money.Create(200, "USD");
    private readonly TimeSpan _returnWindow = TimeSpan.FromDays(14);
    private readonly DateTime _recentCompletedAt = DateTime.UtcNow.AddDays(-3);

    private List<OrderItem> CreateOrderItems()
    {
        var productId = ProductId.CreateUnique();
        var item = OrderItem.Create(productId, 2, Money.Create(100, "USD"));
        item.InitializeOrderItem(_orderId, OrderItemId.From(1));
        return new List<OrderItem> { item };
    }

    private RequestReturn CreatePendingReturnRequest()
    {
        var items = CreateOrderItems();
        return RequestReturn.Create(
            orderId: _orderId,
            customerId: _customerId,
            reason: "Defective product",
            itemsToReturn: items,
            refundAmount: _refundAmount,
            orderCompletedAt: _recentCompletedAt,
            orderItems: items,
            returnWindow: _returnWindow);
    }


    [Fact]
    public void Create_ShouldSucceed_WhenAllParametersAreValid()
    {
        var items = CreateOrderItems();

        var returnRequest = RequestReturn.Create(
            orderId: _orderId,
            customerId: _customerId,
            reason: "Damaged box",
            itemsToReturn: items,
            refundAmount: _refundAmount,
            orderCompletedAt: _recentCompletedAt,
            orderItems: items,
            returnWindow: _returnWindow);

        Assert.NotNull(returnRequest);
        Assert.Equal(_orderId, returnRequest.OrderId);
        Assert.Equal(_customerId, returnRequest.CustomerId);
        Assert.Equal("Damaged box", returnRequest.Reason);
        Assert.Equal(_refundAmount, returnRequest.RefundAmount);
        Assert.Equal(ReturnStatus.Pending, returnRequest.Status);
        Assert.Equal(1, returnRequest.Version);

        var evt = Assert.Single(returnRequest.UncommitedEvents) as ReturnRequestCreatedEvent;
        Assert.NotNull(evt);
        Assert.Equal(_orderId, evt.OrderId);
        Assert.Equal(_customerId, evt.CustomerId);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenReasonIsEmpty()
    {
        var items = CreateOrderItems();

        var ex = Assert.Throws<DomainException>(() =>
            RequestReturn.Create(_orderId, _customerId, "", items, _refundAmount,
                _recentCompletedAt, items, _returnWindow));

        Assert.Contains("Return reason is required", ex.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenReasonIsWhitespace()
    {
        var items = CreateOrderItems();

        var ex = Assert.Throws<DomainException>(() =>
            RequestReturn.Create(_orderId, _customerId, "   ", items, _refundAmount,
                _recentCompletedAt, items, _returnWindow));

        Assert.Contains("Return reason is required", ex.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenItemsToReturnIsEmpty()
    {
        var items = CreateOrderItems();

        var ex = Assert.Throws<DomainException>(() =>
            RequestReturn.Create(_orderId, _customerId, "Reason", new List<OrderItem>(), _refundAmount,
                _recentCompletedAt, items, _returnWindow));

        Assert.Contains("Must specify at least one item", ex.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenRefundAmountIsZero()
    {
        var items = CreateOrderItems();

        var ex = Assert.Throws<DomainException>(() =>
            RequestReturn.Create(_orderId, _customerId, "Reason", items, Money.Default("USD"),
                _recentCompletedAt, items, _returnWindow));

        Assert.Contains("Refund amount must be greater than zero", ex.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenReturnWindowExpired()
    {
        var items = CreateOrderItems();
        var expiredCompletedAt = DateTime.UtcNow.AddDays(-20); // beyond 14-day window

        var ex = Assert.Throws<DomainException>(() =>
            RequestReturn.Create(_orderId, _customerId, "Reason", items, _refundAmount,
                expiredCompletedAt, items, _returnWindow));

        Assert.Contains("Return window expired", ex.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenItemDoesNotBelongToOrder()
    {
        var orderItems = CreateOrderItems();
        var foreignItem = OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(50, "USD"));
        foreignItem.InitializeOrderItem(_orderId, OrderItemId.From(99));

        var ex = Assert.Throws<DomainException>(() =>
            RequestReturn.Create(_orderId, _customerId, "Reason",
                new List<OrderItem> { foreignItem },
                _refundAmount, _recentCompletedAt, orderItems, _returnWindow));

        Assert.Contains("is not part of the order", ex.Message);
    }
    
    [Fact]
    public void MarkAsReceived_ShouldTransitionToReceived_WhenPending()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.ClearUncommittedEvents();

        returnRequest.MarkAsReceived();

        Assert.Equal(ReturnStatus.Received, returnRequest.Status);
        Assert.NotNull(returnRequest.ReceivedAt);

        var evt = Assert.Single(returnRequest.UncommitedEvents) as ReturnItemsReceivedEvent;
        Assert.NotNull(evt);
        Assert.Equal(_orderId, evt.OrderId);
    }

    [Fact]
    public void MarkAsReceived_ShouldThrowException_WhenNotPending()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.MarkAsReceived(); // now Received

        var ex = Assert.Throws<DomainException>(() => returnRequest.MarkAsReceived());

        Assert.Contains("Cannot mark as received in Received status", ex.Message);
    }
    
    [Fact]
    public void ProcessRefund_ShouldTransitionToRefunded_WhenReceived()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.MarkAsReceived();
        returnRequest.ClearUncommittedEvents();

        returnRequest.ProcessRefund("REF-999");

        Assert.Equal(ReturnStatus.Refunded, returnRequest.Status);
        Assert.Equal("REF-999", returnRequest.RefundId);

        var evt = Assert.Single(returnRequest.UncommitedEvents) as ReturnRefundProcessedEvent;
        Assert.NotNull(evt);
        Assert.Equal("REF-999", evt.RefundId);
        Assert.Equal(_refundAmount, evt.RefundAmount);
    }

    [Fact]
    public void ProcessRefund_ShouldThrowException_WhenNotReceived()
    {
        var returnRequest = CreatePendingReturnRequest(); // still Pending

        var ex = Assert.Throws<DomainException>(() => returnRequest.ProcessRefund("REF-123"));

        Assert.Contains("Cannot process refund in Pending status", ex.Message);
    }

    [Fact]
    public void ProcessRefund_ShouldThrowException_WhenRefundIdIsEmpty()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.MarkAsReceived();

        var ex = Assert.Throws<DomainException>(() => returnRequest.ProcessRefund(""));

        Assert.Contains("Refund ID is required", ex.Message);
    }

    [Fact]
    public void ProcessRefund_ShouldThrowException_WhenRefundIdIsWhitespace()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.MarkAsReceived();

        var ex = Assert.Throws<DomainException>(() => returnRequest.ProcessRefund("   "));

        Assert.Contains("Refund ID is required", ex.Message);
    }

    [Fact]
    public void Complete_ShouldTransitionToCompleted_WhenRefunded()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.MarkAsReceived();
        returnRequest.ProcessRefund("REF-DONE");
        returnRequest.ClearUncommittedEvents();

        returnRequest.Complete();

        Assert.Equal(ReturnStatus.Completed, returnRequest.Status);

        var evt = Assert.Single(returnRequest.UncommitedEvents) as ReturnCompletedEvent;
        Assert.NotNull(evt);
        Assert.Equal(_orderId, evt.OrderId);
    }

    [Fact]
    public void Complete_ShouldThrowException_WhenNotRefunded()
    {
        var returnRequest = CreatePendingReturnRequest();
        returnRequest.MarkAsReceived(); // Received, not Refunded

        var ex = Assert.Throws<DomainException>(() => returnRequest.Complete());

        Assert.Contains("Cannot complete return in Received status", ex.Message);
    }

    [Fact]
    public void Complete_ShouldThrowException_WhenStillPending()
    {
        var returnRequest = CreatePendingReturnRequest();

        var ex = Assert.Throws<DomainException>(() => returnRequest.Complete());

        Assert.Contains("Cannot complete return in Pending status", ex.Message);
    }
    
    [Fact]
    public void FromHistory_ShouldRebuildStateExactlyFromHistory()
    {
        var returnRequestId = ReturnRequestId.CreateUnique();
        var items = CreateOrderItems();
        var now = DateTime.UtcNow;

        var history = new List<IDomainEvent>
        {
            new ReturnRequestCreatedEvent(returnRequestId, _orderId, _customerId,
                "Defective", items, _refundAmount, now),
            new ReturnItemsReceivedEvent(returnRequestId, _orderId, _customerId, now.AddDays(1)),
            new ReturnRefundProcessedEvent(returnRequestId, _orderId, _customerId,
                "REF-XYZ", _refundAmount, now.AddDays(2))
        };

        var returnRequest = RequestReturn.FromHistory(history);

        Assert.Equal(returnRequestId, returnRequest.Id);
        Assert.Equal(ReturnStatus.Refunded, returnRequest.Status);
        Assert.Equal("REF-XYZ", returnRequest.RefundId);
        Assert.Equal(3, returnRequest.Version);
        Assert.Empty(returnRequest.UncommitedEvents);
    }

    [Fact]
    public void Version_ShouldIncrementForEachRaisedEvent()
    {
        var returnRequest = CreatePendingReturnRequest();
        Assert.Equal(1, returnRequest.Version);

        returnRequest.MarkAsReceived();
        Assert.Equal(2, returnRequest.Version);

        returnRequest.ProcessRefund("REF-1");
        Assert.Equal(3, returnRequest.Version);

        returnRequest.Complete();
        Assert.Equal(4, returnRequest.Version);
    }
}

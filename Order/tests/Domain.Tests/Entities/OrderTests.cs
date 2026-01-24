using Domain.Common;
using Domain.Entities;
using Domain.Events;
using Domain.Events.CreateOrder;
using Domain.Events.OrderReturn;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class OrderTests
{

    private readonly CustomerId _customerId = CustomerId.CreateUnique();
    private readonly PaymentId _paymentId = PaymentId.From("PaymentId");
    private readonly Address _address = Address.Create("Zizkov 18", "Prague", "Czech Republic", "18000");
    private readonly ProductId _productId = ProductId.CreateUnique();
    private readonly Money _price = Money.Create(100, "USD");

    private List<OrderItem> CreateDefaultItems()
    {
        return new List<OrderItem>() { OrderItem.Create(_productId, 2, _price) };
    }

    private Order CreateCompleteOrder()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        order.Pay(_paymentId);
        order.Approve();
        order.Complete();
        order.MarkEventsAsCommited(); // clear history
        return order;
    }
    
    [Fact]
    public void Create_ShouldCreateOrderSuccessfully()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());

        var evt = Assert.Single(order.UncommitedEvents) as OrderCreatedEvent;
        Assert.NotNull(evt);
        Assert.Equal(_customerId, evt.CustomerId);
        Assert.Equal(Money.Create(200, "USD"), evt.TotalPrice);
        
        
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenOrderItemsIsEmptyOrZero()
    {
        Assert.Throws<OrderDomainException>(() =>
            Order.Create(_customerId, _address, new List<OrderItem>()));
    }

    [Fact]
    public void Pay_WhenPending_ShouldTransitionToPaid()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        order.MarkEventsAsCommited();

        order.Pay(_paymentId);
        
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.IsType<OrderPaidEvent>(order.UncommitedEvents.Last());
        Assert.Equal(2, order.Version);
    }

    [Fact]
    public void Pay_WhenAlreadyPaid_ShouldThrowException()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());

        order.Pay(_paymentId);

        var ex = Assert.Throws<OrderDomainException>(() => order.Pay(_paymentId));
        Assert.Contains("Cannot pay order in Paid status", ex.Message);
    }

    [Fact]
    public void Approve_WhenPaid_ShouldTransitionToApproved()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        
        order.Pay(_paymentId);
        order.MarkEventsAsCommited();
        
        order.Approve();
        
        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.IsType<OrderApprovedEvent>(order.UncommitedEvents.Last());
    }
    
    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrowException()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());

        order.Pay(_paymentId);
        order.Approve();

        var ex = Assert.Throws<OrderDomainException>(() => order.Approve());
        Assert.Contains("Cannot approve order in Approved status", ex.Message);
    }

    [Fact]
    public void Complete_WhenApproved_ShouldTransitionToCompleted()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        
        order.Pay(_paymentId);
        order.Approve();
        
        order.Complete();
        
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.IsType<OrderCompletedEvent>(order.UncommitedEvents.Last());
    }
    
    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrowException()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());

        order.Pay(_paymentId);
        order.Approve();
        
        order.Complete();

        var ex = Assert.Throws<OrderDomainException>(() => order.Complete());
        Assert.Contains("Cannot complete order in Completed status", ex.Message);
    }

    [Fact]
    public void Cancel_WhenPending_ShouldTransitionToCancelled()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());

        var cancelReason = "Racism";
        
        order.Cancel(new List<string> { cancelReason });
        
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.IsType<OrderCancelledEvent>(order.UncommitedEvents.Last());
        var message = Assert.Single(order.FailedMessage);
        Assert.Equal(cancelReason, message);
    }

    [Fact]
    public void Cancel_WithMultiplyMessages_ShouldStoreAllMessages()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        var reason = new List<string> { "Inventory shortage", "Payment gateway timeout" };
        
        order.Cancel(reason);
        
        Assert.Equal(2, order.FailedMessage.Count);
        Assert.Contains("Inventory shortage", order.FailedMessage);
        Assert.Contains("Payment gateway timeout", order.FailedMessage);
    }
    

    [Fact]
    public void Cancel_ShouldNotModifyFailedMessages_WhenTransitionFails()
    {
        var order = CreateCompleteOrder();

        var initialMessageCount = order.FailedMessage.Count;

        Assert.Throws<OrderDomainException>(() =>
            order.Cancel(new List<string> { "Illegal Cancellation" }));
        
        // verify the list was not touched because the exception was thrown before the foreach loop
        Assert.Equal(initialMessageCount, order.FailedMessage.Count);
    }
    
    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowException()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        order.Cancel(new List<string> { "First Reason" }); 

         Assert.Throws<OrderDomainException>(() => 
            order.Cancel(new List<string> { "Second Reason" }));

        var message = Assert.Single(order.FailedMessage);
        Assert.Equal("First Reason", message); 
    
        Assert.DoesNotContain("Second Reason", order.FailedMessage);

        
    }
    
    // return /refund tests

    [Fact]
    public void RequestReturn_WhenCompleted_ShouldTransitionToReturnRequested()
    {
        var order = CreateCompleteOrder();
        var reason = "Damaged Item";
        var itemsToReturn = CreateDefaultItems();
        
        order.RequestReturn(reason, itemsToReturn);
        
        Assert.Equal(OrderStatus.ReturnRequested, order.Status);
        Assert.Equal(reason, order.ReturnReason);
        Assert.Equal(itemsToReturn.Count, order.ReturnedItems.Count);

        var evt = Assert.Single(order.UncommitedEvents) as OrderReturnRequestedEvent;
        Assert.NotNull(evt);
        Assert.Equal(Money.Create(200, "USD"), evt.RefundAmount);
    }

    [Fact]
    public void RequestReturn_WhenNotCompleted_ShouldThrowException()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        order.Pay(_paymentId);

        var ex = Assert.Throws<OrderDomainException>(() =>
            order.RequestReturn("Reason", CreateDefaultItems()));

        Assert.Contains("must be Completed", ex.Message);
    }

    [Fact]
    public void RequestReturn_WhenItemsEmpty_ShouldThrowException()
    {
        var order = CreateCompleteOrder();

        var ex = Assert.Throws<OrderDomainException>(() =>
            order.RequestReturn("Reason", new List<OrderItem>()));

        Assert.Contains("Must specify at least one item", ex.Message);
    }

    [Fact]
    public void RequestReturn_WhenItemsDoNotBelongToOrder_ShouldThrowException()
    {
        var order = CreateCompleteOrder();

        var foreignItem = OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(50, "USD"));

        var ex = Assert.Throws<OrderDomainException>(() =>
            order.RequestReturn("Reason", new List<OrderItem> { foreignItem }));

        Assert.Contains("is not part of this order", ex.Message);
    }

    [Fact]
    public void ProcessRefund_ShouldSuccedd_OnlyWhenStatusIsReturnReceived()
    {
        var order = CreateCompleteOrder();
        order.RequestReturn("Reason", CreateDefaultItems());
        order.ConfirmReturnReceived();
        order.MarkEventsAsCommited();

        var refundAmount = Money.Create(200, "USD");
        order.ProcessRefund("REf-123", refundAmount);
        
        Assert.Equal(OrderStatus.Refunded, order.Status);
        var evt = Assert.IsType<OrderReturnRefundedEvent>(order.UncommitedEvents.Last());
        Assert.Equal(refundAmount, evt.RefundAmount);
    }

    [Fact]
    public void ProcessRefund_ShouldThrowException_WhenItemsNotYetReceived()
    {
        var order = CreateCompleteOrder();
        order.RequestReturn("Reason", CreateDefaultItems());
        
        // skip confirmReturnReceived()

        var ex = Assert.Throws<OrderDomainException>(() =>
            order.ProcessRefund("REF-FAIL", Money.Create(200, "USD")));

        Assert.Contains("Cannot process refund for order", ex.Message);
    }
    
    [Fact]
    public void CompleteReturnReceived_ShouldTransitionToReturnReceived()
    {
        var order = CreateCompleteOrder();
        order.RequestReturn("Reason", CreateDefaultItems());
        order.ConfirmReturnReceived();
        order.ProcessRefund("REF-123", Money.Create(200, "USD"));
        order.MarkEventsAsCommited();

        order.CompleteReturn();

        Assert.Equal(OrderStatus.Returned, order.Status);
        Assert.IsType<OrderReturnCompletedEvent>(order.UncommitedEvents.Last());
    }



    // infra tests
    
    [Fact]
    public void FromEvents_ShouldRebuildStateExactlyFromHistory()
    {
        var orderId = OrderId.CreateUnique();
        var totalPrice = Money.Create(200, "USD");
        var items = CreateDefaultItems();
        var now = DateTime.UtcNow;

        var history = new List<IDomainEvent>
        {
            new OrderCreatedEvent(orderId, _customerId, totalPrice, _address, items, now),
            new OrderPaidEvent(orderId, _customerId, _paymentId, totalPrice, now.AddMinutes(5)),
            new OrderApprovedEvent(orderId, _customerId, now.AddMinutes(30))
        };

        var order = Order.FromEvents(history);
        
        Assert.Equal(orderId, order.Id);
        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.Equal(3, order.Version);
        Assert.Empty(order.UncommitedEvents); // reconstruction shouldn't create "new" events
    }

    [Fact]
    public void MarkEventsAsCommited_ShouldCreateTheList()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        Assert.Single(order.UncommitedEvents);
        
        order.MarkEventsAsCommited();
        
        Assert.Empty(order.UncommitedEvents);
    }

    [Fact]
    public void Version_ShouldIncrementEveryTimeAnEventIsRaised()
    {
        var order = Order.Create(_customerId, _address, CreateDefaultItems());
        order.Pay(_paymentId);
        order.Approve();
        
        Assert.Equal(3, order.Version);
    }
    
    
}
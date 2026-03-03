using Domain.Common;
using Domain.Entities.B2BOrder;
using Domain.Events.B2BOrder;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class B2BOrderTests
{
    private readonly CustomerId _customerId = CustomerId.CreateUnique();
    private readonly Address _address = Address.Create("Wenceslas Square 1", "Prague", "CZ", "11000");
    private readonly ProductId _productId = ProductId.CreateUnique();
    private readonly Money _price = Money.Create(100m, "USD");

    private B2BOrder CreateStartedOrder() =>
        B2BOrder.Start(_customerId, "ACME Corp", _address);

    private B2BOrder CreateOrderWithOneItem()
    {
        var order = CreateStartedOrder();
        order.ClearUncommittedEvents();
        order.AddItem(_productId, 3, _price);
        return order;
    }


    [Fact]
    public void Start_ShouldRaiseB2BOrderStartedEvent()
    {
        var order = CreateStartedOrder();

        var evt = Assert.Single(order.UncommitedEvents) as B2BOrderStartedEvent;
        Assert.NotNull(evt);
        Assert.Equal("ACME Corp", evt.CompanyName);
        Assert.Equal(_customerId, evt.CustomerId);
    }

    [Fact]
    public void Start_ShouldSetStatusToDraftAndIncrementVersion()
    {
        var order = CreateStartedOrder();

        Assert.Equal(B2BOrderStatus.Draft, order.Status);
        Assert.Equal(1, order.Version);
        Assert.Equal("ACME Corp", order.CompanyName);
    }

    [Fact]
    public void Start_ShouldThrow_WhenCompanyNameIsWhitespace()
    {
        Assert.Throws<DomainException>(() => B2BOrder.Start(_customerId, "  ", _address));
    }

    [Fact]
    public void Start_ShouldThrow_WhenCompanyNameIsEmpty()
    {
        Assert.Throws<DomainException>(() => B2BOrder.Start(_customerId, string.Empty, _address));
    }

    [Fact]
    public void AddItem_ShouldRaiseLineItemAddedEvent_AndIncrementVersion()
    {
        var order = CreateStartedOrder();
        order.ClearUncommittedEvents();

        order.AddItem(_productId, 2, _price);

        var evt = Assert.Single(order.UncommitedEvents) as LineItemAddedEvent;
        Assert.NotNull(evt);
        Assert.Equal(_productId, evt.ProductId);
        Assert.Equal(2, evt.Quantity);
        Assert.Equal(_price, evt.UnitPrice);
        Assert.Equal(2, order.Version);
    }

    [Fact]
    public void AddItem_ShouldAppearInActiveItems()
    {
        var order = CreateOrderWithOneItem();

        Assert.Single(order.ActiveItems);
        Assert.Equal(_productId, order.ActiveItems[0].ProductId);
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenProductAlreadyExists()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.AddItem(_productId, 1, _price));
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenNotInDraft()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();
        order.Finalize(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AddItem(ProductId.CreateUnique(), 1, _price));
    }

    [Fact]
    public void RemoveItem_ShouldMarkItemRemovedAndRaiseEvent()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();

        order.RemoveItem(_productId);

        Assert.Empty(order.ActiveItems);
        Assert.IsType<LineItemRemovedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void RemoveItem_ShouldThrow_WhenProductNotFound()
    {
        var order = CreateStartedOrder();
        order.ClearUncommittedEvents();

        Assert.Throws<DomainException>(() => order.RemoveItem(ProductId.CreateUnique()));
    }

    [Fact]
    public void ChangeItemQuantity_ShouldUpdateQuantity()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();

        order.ChangeItemQuantity(_productId, 10);

        Assert.Equal(10, order.ActiveItems[0].Quantity);
        Assert.IsType<LineItemQuantityChangedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void ChangeItemQuantity_ShouldThrow_WhenQuantityIsZero()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.ChangeItemQuantity(_productId, 0));
    }

    [Fact]
    public void ChangeItemQuantity_ShouldThrow_WhenQuantityIsNegative()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.ChangeItemQuantity(_productId, -5));
    }

    [Fact]
    public void AdjustItemPrice_ShouldSetAdjustedUnitPrice()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();

        var adjustedPrice = Money.Create(80m, "USD");
        order.AdjustItemPrice(_productId, adjustedPrice);

        Assert.Equal(80m, order.ActiveItems[0].AdjustedUnitPrice?.Amount);
        Assert.IsType<LineItemPriceAdjustedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void TotalPrice_ShouldComputeCorrectly_WithNoDiscount()
    {
        // 3 items * $100 = $300
        var order = CreateOrderWithOneItem();

        Assert.Equal(300m, order.TotalPrice.Amount);
        Assert.Equal("USD", order.TotalPrice.Currency);
    }

    [Fact]
    public void TotalPrice_ShouldApplyDiscountCorrectly()
    {
        // 3 items * $100 - 10% = $270
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();
        order.ApplyDiscount(10);

        Assert.Equal(270m, order.TotalPrice.Amount);
    }

    [Fact]
    public void TotalPrice_ShouldUseAdjustedUnitPrice_WhenSet()
    {
        // 3 items, adjusted to $50 each => $150
        var order = CreateOrderWithOneItem();
        order.AdjustItemPrice(_productId, Money.Create(50m, "USD"));

        Assert.Equal(150m, order.TotalPrice.Amount);
    }

    [Fact]
    public void TotalPrice_ShouldReturnDefault_WhenNoActiveItems()
    {
        var order = CreateStartedOrder();

        Assert.Equal(0m, order.TotalPrice.Amount);
    }

    [Fact]
    public void ApplyDiscount_ShouldRaiseDiscountAppliedEvent()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();

        order.ApplyDiscount(15m);

        var evt = Assert.Single(order.UncommitedEvents) as DiscountAppliedEvent;
        Assert.NotNull(evt);
        Assert.Equal(15m, evt.DiscountPercent);
        Assert.Equal(15m, order.DiscountPercent);
    }

    [Fact]
    public void ApplyDiscount_ShouldThrow_WhenDiscountIsNegative()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.ApplyDiscount(-1));
    }

    [Fact]
    public void ApplyDiscount_ShouldThrow_WhenDiscountExceeds100()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.ApplyDiscount(101));
    }

    [Fact]
    public void AddComment_ShouldAppendFormattedComment()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();

        order.AddComment("Alice", "Please review the adjusted pricing");

        Assert.Single(order.Comments);
        Assert.Contains("Alice", order.Comments[0]);
        Assert.Contains("Please review the adjusted pricing", order.Comments[0]);
        Assert.IsType<CommentAddedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void AddComment_ShouldThrow_WhenAuthorIsEmpty()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.AddComment(string.Empty, "text"));
    }

    [Fact]
    public void AddComment_ShouldThrow_WhenTextIsEmpty()
    {
        var order = CreateOrderWithOneItem();

        Assert.Throws<DomainException>(() => order.AddComment("Alice", string.Empty));
    }

    [Fact]
    public void ChangeDeliveryDate_ShouldUpdateRequestedDeliveryDate()
    {
        var order = CreateStartedOrder();
        order.ClearUncommittedEvents();
        var targetDate = DateTime.UtcNow.AddDays(30);

        order.ChangeDeliveryDate(targetDate);

        Assert.Equal(targetDate, order.RequestedDeliveryDate);
        Assert.IsType<DeliveryDateChangedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void ChangeDeliveryDate_ShouldAllowNull()
    {
        var order = CreateStartedOrder();
        order.ChangeDeliveryDate(DateTime.UtcNow.AddDays(10));
        order.ClearUncommittedEvents();

        order.ChangeDeliveryDate(null);

        Assert.Null(order.RequestedDeliveryDate);
    }

    [Fact]
    public void ChangeDeliveryAddress_ShouldUpdateAddress()
    {
        var order = CreateStartedOrder();
        order.ClearUncommittedEvents();
        var newAddress = Address.Create("New St 99", "Brno", "CZ", "60200");

        order.ChangeDeliveryAddress(newAddress);

        Assert.Equal("New St 99", order.DeliveryAddress.Street);
        Assert.IsType<DeliveryAddressChangedEvent>(order.UncommitedEvents.Last());
    }

    [Fact]
    public void Finalize_ShouldRaiseQuoteFinalizedEvent()
    {
        var order = CreateOrderWithOneItem();
        order.ClearUncommittedEvents();
        var createdOrderId = Guid.NewGuid();

        order.Finalize(createdOrderId);

        var evt = Assert.Single(order.UncommitedEvents) as QuoteFinalizedEvent;
        Assert.NotNull(evt);
        Assert.Equal(createdOrderId, evt.FinalizedOrderId);
        Assert.Equal(B2BOrderStatus.Finalized, order.Status);
    }

    [Fact]
    public void Finalize_ShouldThrow_WhenAllItemsAreRemoved()
    {
        var order = CreateOrderWithOneItem();
        order.RemoveItem(_productId);
        order.ClearUncommittedEvents();

        Assert.Throws<DomainException>(() => order.Finalize(Guid.NewGuid()));
    }

    [Fact]
    public void Finalize_ShouldThrow_WhenNotInDraft()
    {
        var order = CreateOrderWithOneItem();
        order.Cancel(new List<string> { "reason" });
        order.ClearUncommittedEvents();

        Assert.Throws<DomainException>(() => order.Finalize(Guid.NewGuid()));
    }

    [Fact]
    public void Cancel_ShouldRaiseB2BOrderCancelledEvent()
    {
        var order = CreateStartedOrder();
        order.ClearUncommittedEvents();
        var reasons = new List<string> { "Budget cut", "Vendor changed" };

        order.Cancel(reasons);

        var evt = Assert.Single(order.UncommitedEvents) as B2BOrderCancelledEvent;
        Assert.NotNull(evt);
        Assert.Equal(2, evt.Reasons.Count);
        Assert.Equal(B2BOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyCancelled()
    {
        var order = CreateStartedOrder();
        order.Cancel(new List<string> { "First reason" });
        order.ClearUncommittedEvents();

        Assert.Throws<DomainException>(() => order.Cancel(new List<string> { "Second" }));
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenFinalized()
    {
        var order = CreateOrderWithOneItem();
        order.Finalize(Guid.NewGuid());
        order.ClearUncommittedEvents();

        Assert.Throws<DomainException>(() => order.Cancel(new List<string> { "Too late" }));
    }

    [Fact]
    public void FromHistory_ShouldRebuildStateCorrectlyFromEvents()
    {
        var b2bOrderId = B2BOrderId.CreateUnique();
        var productId = ProductId.CreateUnique();
        var itemId = QuoteLineItemId.CreateUnique();
        var now = DateTime.UtcNow;

        var history = new List<IDomainEvent>
        {
            new B2BOrderStartedEvent(b2bOrderId, _customerId, "ACME", _address, now),
            new LineItemAddedEvent(b2bOrderId, itemId, productId, 5, Money.Create(50m, "USD"), now.AddSeconds(1)),
            new DiscountAppliedEvent(b2bOrderId, 10m, now.AddSeconds(2)),
        };

        var order = B2BOrder.FromHistory(history);

        Assert.Equal(b2bOrderId, order.Id);
        Assert.Equal(B2BOrderStatus.Draft, order.Status);
        Assert.Equal(3, order.Version);
        Assert.Single(order.ActiveItems);
        Assert.Equal(5, order.ActiveItems[0].Quantity);
        Assert.Equal(225m, order.TotalPrice.Amount); // 5*50 - 10% = 225
        Assert.Empty(order.UncommitedEvents); // replay must not add new events
    }

    [Fact]
    public void Snapshot_ShouldRoundTripAllFields()
    {
        var order = CreateOrderWithOneItem();
        order.ApplyDiscount(5m);
        order.AddComment("Bobenko Serhiy", "Looks good type shit");
        order.ChangeDeliveryDate(DateTime.UtcNow.AddDays(14));
        order.ClearUncommittedEvents();

        var state = order.ToSnapshotState();
        var restored = B2BOrder.FromSnapshot(state);

        Assert.Equal(order.Id, restored.Id);
        Assert.Equal(order.CustomerId, restored.CustomerId);
        Assert.Equal(order.CompanyName, restored.CompanyName);
        Assert.Equal(order.Status, restored.Status);
        Assert.Equal(order.DiscountPercent, restored.DiscountPercent);
        Assert.Equal(order.Version, restored.Version);
        Assert.Single(restored.ActiveItems);
        Assert.Single(restored.Comments);
        Assert.Equal(order.RequestedDeliveryDate, restored.RequestedDeliveryDate);
        Assert.Empty(restored.UncommitedEvents);
    }

    [Fact]
    public void Version_ShouldIncrementForEveryRaisedEvent()
    {
        var order = CreateStartedOrder();      // v=1
        order.AddItem(_productId, 1, _price);  // v=2
        order.ApplyDiscount(5);                // v=3

        Assert.Equal(3, order.Version);
    }

    [Fact]
    public void ClearUncommittedEvents_ShouldEmptyTheList()
    {
        var order = CreateStartedOrder();
        Assert.Single(order.UncommitedEvents);

        order.ClearUncommittedEvents();

        Assert.Empty(order.UncommitedEvents);
    }
}

using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Order : AggregateRoot<OrderId>
{
    public CustomerId CustomerId { get; private set; }
    public TrackingId TrackingId { get; private set; }

    private readonly List<OrderItem> _items = new();

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public Address DeliveryAddress { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // shipment
    // payment

    private Order() : base() {}

    private Order(
        OrderId orderId,
        CustomerId customerId,
        Money totalPrice,
        Address address,
        List<OrderItem> items,
        TrackingId? trackingId = null,
        OrderStatus? status = null
        ) : base(orderId)
    {
        CustomerId = customerId;
        TrackingId = trackingId ?? TrackingId.CreateUnique();
        TotalPrice = totalPrice;
        _items = items;
        DeliveryAddress = address;
        Status = status ?? OrderStatus.Pending;
    }

    public Order Create(
        CustomerId customerId,
        Address address,
        List<OrderItem> items)
    {
     
        ValidateItems(items);
        var totalPrice = CalculateTotalPrice(items);
        
        
        var order = new Order(
            OrderId.CreateUnique(),
            customerId,
            totalPrice,
            address,
            items
        );
        
        order.ValidateOrder();
        order.InitializeOrder();

        return order;
    }

    public void InitializeOrder()
    {
        ValidateInitialOrder();

        Id = OrderId.CreateUnique();
        TrackingId = TrackingId.CreateUnique();
        Status = OrderStatus.Pending;
        
        InitializeOrderItems();
        
        AddDomainEvent(new OrderCreatedEvent(
            Id,
            CustomerId,
            TotalPrice,
            DeliveryAddress,
            Items.ToList(),
            CreatedAt));
    }

    public void Pay()
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.AwaitingPayment)
            throw new OrderDomainException(
                $"Order is not in correct state for pay operation. Current status: {Status}");

        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrderPaidEvent(Id, CustomerId, TotalPrice, DateTime.UtcNow));
    }

    public void Approve()
    {
        if (Status != OrderStatus.Paid)
            throw new OrderDomainException(
                $"Order is not in correct state for approve operation. Current state: {Status}");

        Status = OrderStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrderApprovedEvent(Id, CustomerId, DateTime.UtcNow));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Approved)
            throw new OrderDomainException(
                $"Order is not in correct state for complete operation. Current state: {Status}");

        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new OrderCompletedEvent(Id, CustomerId, DateTime.UtcNow));
    }

    public void InitiateCancel()
    {
        if (Status != OrderStatus.Paid && Status != OrderStatus.Approved)
            throw new OrderDomainException(
                $"Order is not in correct state for cancel initiation. Current state: {Status}");

        Status = OrderStatus.Cancelling;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Cancelled && Status != OrderStatus.Pending)
            throw new OrderDomainException($"Order is not correct state for cancel operation. Current state: {Status}");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
    
    

    public void ValidateOrder()
    {
        ValidateInitialOrder();
        ValidateTotalPrice();
        ValidateItemsPrice();
    }
    

    private static void ValidateItems(List<OrderItem> items)
    {
        if (items.Count == 0)
            throw new OrderDomainException("Order must have at least one item.");
        
        var baseCurrency = items[0].PriceAtPurchase.Currency;
        if (items.Any(x => x.PriceAtPurchase.Currency != baseCurrency))
            throw new OrderDomainException("All items must share the same currency.");
    }

    private void ValidateInitialOrder()
    {
        if (Status != OrderStatus.Pending && Id != null)
            throw new OrderDomainException("Order is not in correct state to be initializated");
    }

    private void ValidateTotalPrice()
    {
        if (!TotalPrice.IsGreaterThenZero())
            throw new OrderDomainException("Total price must be greater then zero");
    }

    private void ValidateItemsPrice()
    {
        var calculatedTotal = Items
            .Select(item =>
            {
                ValidateItemPrice(item);
                return item.GetSubTotal();
            })
            .Aggregate(Money.Default(TotalPrice.Currency), (acc, price) => acc.Add(price));

        if (calculatedTotal != TotalPrice)
            throw new OrderDomainException(
                $"Total price {TotalPrice} is not equal to the sum of order items prices {calculatedTotal}");
    }

    private void InitializeOrderItems()
    {
        long itemId = 1;
        foreach (var item in Items)
        {
            item.InitializeOrderItem(Id, OrderItemId.From(itemId++));
        }
    }

    private static void ValidateItemPrice(OrderItem item)
    {
        if (!item.IsPriceValid())
            throw new OrderDomainException($"Order item price is not valid for product {item.ProductId}");
    }

    private static Money CalculateTotalPrice(List<OrderItem> items)
    {
        if (items.Count == 0)
            return Money.Default();

        var currency = items[0].PriceAtPurchase.Currency;

        return items
            .Select(item => item.GetSubTotal())
            .Aggregate(Money.Default(currency), (acc, price) => acc.Add(price));

    }
}
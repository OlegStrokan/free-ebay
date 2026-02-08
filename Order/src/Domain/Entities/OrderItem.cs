using Domain.Common;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class OrderItem : Entity<OrderItemId>
{
    public OrderId? OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money PriceAtPurchase { get; private set; }

    private OrderItem(
        OrderItemId id,
        OrderId? orderId,
        ProductId productId,
        int quantity,
        Money price) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        PriceAtPurchase = price;
    }

    public OrderItem() : base() {}

    public static OrderItem Create(ProductId productId, int quantity, Money price)
    {
        
        ValidatePrice(price);
        ValidateQuantity(quantity);
        return new OrderItem(
            //orderItem can't exist without Order, we lock it
            OrderItemId.From(0), // will be set during order initialization
            null, // will be set during order initialization
            productId,
            quantity,
            price
        );
    }

    internal void InitializeOrderItem(OrderId orderId, OrderItemId orderItemId)
    {
        if (OrderId != null)
            throw new DomainException("OrderItem is already initialized");

        Id = orderItemId;
        OrderId = orderId;
    }

    public void UpdateQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
    }

    public void UpdatePrice(Money newPrice)
    {
        ValidatePrice(newPrice);
        PriceAtPurchase = newPrice;
    }

    public Money GetSubTotal()
    {
        return PriceAtPurchase.Multiply(Quantity);
    }

    public bool IsPriceValid()
    {
        return PriceAtPurchase.IsGreaterThenZero();
    }
        
    private static void ValidatePrice(Money price)
    {
        if (!price.IsGreaterThenZero())
            throw new DomainException($"Order item price should be greater then zero. Got {price}");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"Order item quantity should be greater then zero. Got {quantity}");
    }
}

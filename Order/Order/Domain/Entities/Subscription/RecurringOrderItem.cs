using Domain.ValueObjects;

namespace Domain.Entities.Subscription;

public sealed class RecurringOrderItem
{
    public ProductId ProductId { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money Price { get; private set; } = null!;

    private RecurringOrderItem() { }

    public static RecurringOrderItem Create(ProductId productId, int quantity, Money price)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        return new RecurringOrderItem { ProductId = productId, Quantity = quantity, Price = price };
    }
}

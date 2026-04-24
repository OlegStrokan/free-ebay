using Domain.ValueObjects;

namespace Domain.Entities.B2BOrder;

public sealed class QuoteLineItem
{
    public QuoteLineItemId Id { get; }
    public ProductId ProductId { get; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    // null means no manual override. "saleshead" did not touch this item's price
    public Money? AdjustedUnitPrice { get; private set; }
    public bool IsRemoved { get; private set; }

    public Money EffectiveUnitPrice => AdjustedUnitPrice ?? UnitPrice;
    public Money LineTotal => EffectiveUnitPrice.Multiply(Quantity);

    private QuoteLineItem(QuoteLineItemId id, ProductId productId, int quantity, Money unitPrice)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // Used by the aggregate's Apply(LineItemAddedEvent) - ID comes from the event so it is stable on replay
    internal static QuoteLineItem Restore(QuoteLineItemId id, ProductId productId, int quantity, Money unitPrice)
        => new(id, productId, quantity, unitPrice);

    internal void ChangeQuantity(int quantity) => Quantity = quantity;
    internal void AdjustPrice(Money price) => AdjustedUnitPrice = price;
    internal void Remove() => IsRemoved = true;

    public QuoteLineItemSnapshotState ToSnapshotState() => new(
        Id: Id.Value,
        ProductId: ProductId.Value,
        Quantity: Quantity,
        UnitPrice: UnitPrice.Amount,
        AdjustedUnitPrice: AdjustedUnitPrice?.Amount,
        Currency: UnitPrice.Currency,
        IsRemoved: IsRemoved);

    public static QuoteLineItem FromSnapshot(QuoteLineItemSnapshotState state)
    {
        var item = new QuoteLineItem(
            QuoteLineItemId.From(state.Id),
            ProductId.From(state.ProductId),
            state.Quantity,
            Money.Create(state.UnitPrice, state.Currency));

        if (state.AdjustedUnitPrice.HasValue)
            item.AdjustPrice(Money.Create(state.AdjustedUnitPrice.Value, state.Currency));

        if (state.IsRemoved)
            item.Remove();

        return item;
    }
}

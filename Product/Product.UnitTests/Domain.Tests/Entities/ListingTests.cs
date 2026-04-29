using Domain.Entities;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

[TestFixture]
public sealed class ListingTests
{
    private CatalogItemId _catalogItemId;
    private SellerId _sellerId;
    private Money _price;

    [SetUp]
    public void SetUp()
    {
        _catalogItemId = CatalogItemId.CreateUnique();
        _sellerId = SellerId.CreateUnique();
        _price = Money.Create(1299.99m, "USD");
    }

    private Listing CreateActiveListing(int stock = 5)
    {
        var listing = Listing.Create(_catalogItemId, _sellerId, _price, stock, ListingCondition.New, "box included");
        listing.ClearDomainEvents();
        return listing;
    }

    [Test]
    public void Create_WithStock_ShouldStartActive()
    {
        var listing = Listing.Create(_catalogItemId, _sellerId, _price, 5, ListingCondition.New, null);

        Assert.That(listing.Status, Is.EqualTo(ListingStatus.Active));
        Assert.That(listing.DomainEvents.OfType<ListingCreatedEvent>(), Has.Exactly(1).Items);
    }

    [Test]
    public void Create_WithZeroStock_ShouldStartOutOfStock()
    {
        var listing = Listing.Create(_catalogItemId, _sellerId, _price, 0, ListingCondition.New, null);

        Assert.That(listing.Status, Is.EqualTo(ListingStatus.OutOfStock));
    }

    [Test]
    public void AdjustStock_NegativeDeltaExceedingStock_ShouldClampToZeroAndSetOutOfStock()
    {
        var listing = CreateActiveListing(5);

        listing.AdjustStock(-100);

        Assert.That(listing.StockQuantity, Is.EqualTo(0));
        Assert.That(listing.Status, Is.EqualTo(ListingStatus.OutOfStock));
        var stockEvent = listing.DomainEvents.OfType<ListingStockChangedEvent>().Single();
        Assert.That(stockEvent.PreviousQuantity, Is.EqualTo(5));
        Assert.That(stockEvent.NewQuantity, Is.EqualTo(0));
    }

    [Test]
    public void AdjustStock_OutOfStockListingPositiveDelta_ShouldReactivate()
    {
        var listing = CreateActiveListing(5);
        listing.AdjustStock(-5);
        listing.ClearDomainEvents();

        listing.AdjustStock(2);

        Assert.That(listing.StockQuantity, Is.EqualTo(2));
        Assert.That(listing.Status, Is.EqualTo(ListingStatus.Active));
        Assert.That(listing.DomainEvents.OfType<ListingStatusChangedEvent>().Single().NewStatus, Is.EqualTo("Active"));
    }

    [Test]
    public void ChangePrice_ShouldRaisePriceChangedEvent()
    {
        var listing = CreateActiveListing();
        var newPrice = Money.Create(1199.99m, "USD");

        listing.ChangePrice(newPrice);

        Assert.That(listing.Price, Is.EqualTo(newPrice));
        Assert.That(listing.DomainEvents.OfType<ListingPriceChangedEvent>().Single().NewPrice, Is.EqualTo(newPrice));
    }

    [Test]
    public void DeletedListing_ShouldRejectStockAndPriceChanges()
    {
        var listing = CreateActiveListing();
        listing.Delete();

        Assert.Throws<InvalidProductOperationException>(() => listing.AdjustStock(-1));
        Assert.Throws<InvalidProductOperationException>(() => listing.ChangePrice(Money.Create(10m, "USD")));
    }
}
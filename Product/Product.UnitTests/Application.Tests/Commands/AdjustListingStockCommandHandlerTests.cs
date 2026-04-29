using Application.Commands.AdjustListingStock;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class AdjustListingStockCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private AdjustListingStockCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new AdjustListingStockCommandHandler(_persistence);
    }

    private static Listing CreateActiveListing(int stock = 5)
    {
        var listing = Listing.Create(
            CatalogItemId.CreateUnique(), SellerId.CreateUnique(),
            Money.Create(50m, "USD"), stock, ListingCondition.New, null);
        listing.ClearDomainEvents();
        return listing;
    }

    [Test]
    public async Task Handle_ShouldAdjustStock_WhenListingExists()
    {
        var listing = CreateActiveListing(5);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(
            new AdjustListingStockCommand(listing.Id.Value, 3), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.StockQuantity, Is.EqualTo(8));
        await _persistence.Received(1).UpdateListingAsync(listing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenListingNotFound()
    {
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);

        var result = await _handler.Handle(
            new AdjustListingStockCommand(Guid.NewGuid(), 3), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive().UpdateListingAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NegativeDeltaBeyondStock_ShouldClampToZero()
    {
        var listing = CreateActiveListing(2);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(
            new AdjustListingStockCommand(listing.Id.Value, -10), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.StockQuantity, Is.EqualTo(0));
    }
}

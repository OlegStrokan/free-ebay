using Application.Commands.UpdateListingStock;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class UpdateListingStockCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private UpdateListingStockCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new UpdateListingStockCommandHandler(_persistence);
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
    public async Task Handle_ShouldUpdateStock_WhenListingExists()
    {
        var listing = CreateActiveListing(5);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(
            new UpdateListingStockCommand(listing.Id.Value, 10), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.StockQuantity, Is.EqualTo(10));
        await _persistence.Received(1).UpdateListingAsync(listing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenListingNotFound()
    {
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);

        var result = await _handler.Handle(
            new UpdateListingStockCommand(Guid.NewGuid(), 10), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive().UpdateListingAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenNegativeQuantity()
    {
        var listing = CreateActiveListing(5);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(
            new UpdateListingStockCommand(listing.Id.Value, -1), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("negative"));
    }

    [Test]
    public async Task Handle_SetToZero_ShouldTransitionToOutOfStock()
    {
        var listing = CreateActiveListing(5);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(
            new UpdateListingStockCommand(listing.Id.Value, 0), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.StockQuantity, Is.EqualTo(0));
        Assert.That(listing.Status, Is.EqualTo(ListingStatus.OutOfStock));
    }
}

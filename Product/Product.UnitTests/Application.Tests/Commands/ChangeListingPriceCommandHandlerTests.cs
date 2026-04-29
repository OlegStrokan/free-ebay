using Application.Commands.ChangeListingPrice;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class ChangeListingPriceCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private ChangeListingPriceCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new ChangeListingPriceCommandHandler(_persistence);
    }

    private static Listing CreateActiveListing()
    {
        var listing = Listing.Create(
            CatalogItemId.CreateUnique(), SellerId.CreateUnique(),
            Money.Create(50m, "USD"), 3, ListingCondition.New, null);
        listing.ClearDomainEvents();
        return listing;
    }

    [Test]
    public async Task Handle_ShouldChangePrice_WhenListingExists()
    {
        var listing = CreateActiveListing();
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(
            new ChangeListingPriceCommand(listing.Id.Value, 99m, "USD"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.Price.Amount, Is.EqualTo(99m));
        Assert.That(listing.Price.Currency, Is.EqualTo("USD"));
        await _persistence.Received(1).UpdateListingAsync(listing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenListingNotFound()
    {
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);

        var result = await _handler.Handle(
            new ChangeListingPriceCommand(Guid.NewGuid(), 99m, "USD"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive().UpdateListingAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }
}

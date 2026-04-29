using Application.Commands.DeleteListing;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class DeleteListingCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private DeleteListingCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new DeleteListingCommandHandler(_persistence);
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
    public async Task Handle_ShouldReturnSuccess_AndDeleteListing()
    {
        var listing = CreateActiveListing();
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(new DeleteListingCommand(listing.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.Status, Is.EqualTo(ListingStatus.Deleted));
        await _persistence.Received(1).UpdateListingAsync(listing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenListingNotFound()
    {
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);

        var result = await _handler.Handle(new DeleteListingCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive().UpdateListingAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AlreadyDeletedListing_ShouldReturnFailure()
    {
        var listing = CreateActiveListing();
        listing.Delete();
        listing.ClearDomainEvents();
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);

        var result = await _handler.Handle(new DeleteListingCommand(listing.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Cannot transition"));
    }
}

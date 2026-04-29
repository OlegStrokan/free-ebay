using Application.Commands.UpdateCatalogItemAndListing;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class UpdateCatalogItemAndListingCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private UpdateCatalogItemAndListingCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new UpdateCatalogItemAndListingCommandHandler(_persistence);
    }

    private static Listing CreateActiveListing(CatalogItemId catalogItemId)
    {
        var listing = Listing.Create(
            catalogItemId, SellerId.CreateUnique(),
            Money.Create(50m, "USD"), 3, ListingCondition.New, null);
        listing.ClearDomainEvents();
        return listing;
    }

    private static CatalogItem CreateCatalogItem(string? gtin = null)
    {
        var item = CatalogItem.Create("Old Name", "Old Desc", CategoryId.CreateUnique(), gtin, [], []);
        item.ClearDomainEvents();
        return item;
    }

    private static UpdateCatalogItemAndListingCommand ValidCommand(Guid listingId, string? gtin = null) =>
        new(listingId, "New Name", "New Desc", Guid.NewGuid(),
            99m, "USD", [], [], gtin, "New", null);

    [Test]
    public async Task Handle_ShouldUpdateCatalogItemAndListing_WhenFound()
    {
        var catalogItem = CreateCatalogItem();
        var listing = CreateActiveListing(catalogItem.Id);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);
        _persistence.GetCatalogItemByIdAsync(catalogItem.Id, Arg.Any<CancellationToken>()).Returns(catalogItem);

        var result = await _handler.Handle(ValidCommand(listing.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(catalogItem.Name, Is.EqualTo("New Name"));
        Assert.That(listing.Price.Amount, Is.EqualTo(99m));
        await _persistence.Received(1)
            .UpdateCatalogItemWithListingAsync(catalogItem, listing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenListingNotFound()
    {
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);

        var result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive()
            .UpdateCatalogItemWithListingAsync(
                Arg.Any<CatalogItem>(), Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenCatalogItemNotFound()
    {
        var catalogItem = CreateCatalogItem();
        var listing = CreateActiveListing(catalogItem.Id);
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);
        _persistence
            .GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>())
            .Returns((CatalogItem?)null);

        var result = await _handler.Handle(ValidCommand(listing.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive()
            .UpdateCatalogItemWithListingAsync(
                Arg.Any<CatalogItem>(), Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithGtin_ShouldReturnFailure_WhenGtinTakenByDifferentItem()
    {
        var catalogItem = CreateCatalogItem();
        var listing = CreateActiveListing(catalogItem.Id);
        var otherItem = CreateCatalogItem("012345678901");
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);
        _persistence.GetCatalogItemByIdAsync(catalogItem.Id, Arg.Any<CancellationToken>()).Returns(catalogItem);
        _persistence
            .GetCatalogItemByGtinAsync("012345678901", Arg.Any<CancellationToken>())
            .Returns(otherItem);

        var result = await _handler.Handle(
            new UpdateCatalogItemAndListingCommand(
                listing.Id.Value, "New Name", "New Desc", Guid.NewGuid(),
                99m, "USD", [], [], "012345678901", null, null),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("already exists"));
        await _persistence.DidNotReceive()
            .UpdateCatalogItemWithListingAsync(
                Arg.Any<CatalogItem>(), Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NullCondition_ShouldPreserveExistingCondition()
    {
        var catalogItem = CreateCatalogItem();
        var listing = CreateActiveListing(catalogItem.Id);
        var originalCondition = listing.Condition;
        _persistence.GetListingByIdAsync(Arg.Any<ListingId>(), Arg.Any<CancellationToken>()).Returns(listing);
        _persistence.GetCatalogItemByIdAsync(catalogItem.Id, Arg.Any<CancellationToken>()).Returns(catalogItem);

        var result = await _handler.Handle(
            new UpdateCatalogItemAndListingCommand(
                listing.Id.Value, "New Name", "New Desc", Guid.NewGuid(),
                99m, "USD", [], [], null, null, null),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(listing.Condition, Is.EqualTo(originalCondition));
    }
}

using Application.Commands.CreateListing;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class CreateListingCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private CreateListingCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new CreateListingCommandHandler(_persistence);
    }

    [Test]
    public async Task Handle_ShouldCreateListing_WhenCatalogItemExistsAndSellerHasNoActiveListing()
    {
        var catalogItem = CatalogItem.Create("Sony A7 IV", "Camera", CategoryId.CreateUnique(), null, [], []);
        _persistence
            .GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>())
            .Returns(catalogItem);
        _persistence
            .ActiveListingExistsAsync(Arg.Any<CatalogItemId>(), Arg.Any<SellerId>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(
            new CreateListingCommand(catalogItem.Id.Value, Guid.NewGuid(), 1200m, "USD", 5, "New", null),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.EqualTo(Guid.Empty));
        await _persistence.Received(1).CreateListingAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldFail_WhenSellerAlreadyHasNonDeletedListingForCatalogItem()
    {
        var catalogItem = CatalogItem.Create("Sony A7 IV", "Camera", CategoryId.CreateUnique(), null, [], []);
        _persistence
            .GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>())
            .Returns(catalogItem);
        _persistence
            .ActiveListingExistsAsync(Arg.Any<CatalogItemId>(), Arg.Any<SellerId>(), null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new CreateListingCommand(catalogItem.Id.Value, Guid.NewGuid(), 1200m, "USD", 5, "New", null),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("already has"));
        await _persistence.DidNotReceive().CreateListingAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }
}
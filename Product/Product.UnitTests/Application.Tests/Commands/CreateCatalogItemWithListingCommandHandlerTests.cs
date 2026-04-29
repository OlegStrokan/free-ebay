using Application.Commands.CreateCatalogItemWithListing;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class CreateCatalogItemWithListingCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private CreateCatalogItemWithListingCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new CreateCatalogItemWithListingCommandHandler(_persistence);
    }

    private static CreateCatalogItemWithListingCommand ValidCommand(string? gtin = null) =>
        new(Guid.NewGuid(), "Sony A7 IV", "Camera", Guid.NewGuid(),
            1200m, "USD", 5, [], [], gtin, "New", null);

    [Test]
    public async Task Handle_ShouldCreateCatalogItemAndListing_WhenNoGtinConflict()
    {
        _persistence
            .GetCatalogItemByGtinAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CatalogItem?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.EqualTo(Guid.Empty));
        await _persistence.Received(1)
            .CreateCatalogItemWithListingAsync(
                Arg.Any<CatalogItem>(), Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithGtin_ShouldReturnFailure_WhenGtinAlreadyExists()
    {
        var existing = CatalogItem.Create("Other", "Desc", CategoryId.CreateUnique(), "012345678901", [], []);
        _persistence
            .GetCatalogItemByGtinAsync("012345678901", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(ValidCommand("012345678901"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("already exists"));
        await _persistence.DidNotReceive()
            .CreateCatalogItemWithListingAsync(
                Arg.Any<CatalogItem>(), Arg.Any<Listing>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NullGtin_ShouldSkipGtinCheck()
    {
        var result = await _handler.Handle(ValidCommand(null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _persistence.DidNotReceive()
            .GetCatalogItemByGtinAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnListingId_NotCatalogItemId()
    {
        Listing? capturedListing = null;
        await _persistence.CreateCatalogItemWithListingAsync(
            Arg.Any<CatalogItem>(),
            Arg.Do<Listing>(l => capturedListing = l),
            Arg.Any<CancellationToken>());

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(capturedListing!.Id.Value));
    }
}

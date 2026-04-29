using Application.Commands.UpdateCatalogItem;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class UpdateCatalogItemCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private UpdateCatalogItemCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new UpdateCatalogItemCommandHandler(_persistence);
    }

    private static CatalogItem CreateCatalogItem(string? gtin = null)
    {
        var item = CatalogItem.Create("Old Name", "Old Desc", CategoryId.CreateUnique(), gtin, [], []);
        item.ClearDomainEvents();
        return item;
    }

    private static UpdateCatalogItemCommand ValidCommand(Guid catalogItemId, string? gtin = null) =>
        new(catalogItemId, "New Name", "New Desc", Guid.NewGuid(), gtin, [], []);

    [Test]
    public async Task Handle_ShouldUpdateCatalogItem_WhenFound()
    {
        var item = CreateCatalogItem();
        _persistence.GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(ValidCommand(item.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(item.Name, Is.EqualTo("New Name"));
        await _persistence.Received(1).UpdateCatalogItemAsync(item, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenCatalogItemNotFound()
    {
        _persistence
            .GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>())
            .Returns((CatalogItem?)null);

        var result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
        await _persistence.DidNotReceive().UpdateCatalogItemAsync(Arg.Any<CatalogItem>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithGtin_ShouldReturnFailure_WhenGtinTakenByDifferentItem()
    {
        var item = CreateCatalogItem();
        var otherItem = CreateCatalogItem("012345678901");
        _persistence.GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>()).Returns(item);
        _persistence
            .GetCatalogItemByGtinAsync("012345678901", Arg.Any<CancellationToken>())
            .Returns(otherItem);

        var result = await _handler.Handle(ValidCommand(item.Id.Value, "012345678901"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("already exists"));
        await _persistence.DidNotReceive().UpdateCatalogItemAsync(Arg.Any<CatalogItem>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithGtin_ShouldSucceed_WhenGtinBelongsToSameItem()
    {
        var item = CreateCatalogItem("012345678901");
        _persistence.GetCatalogItemByIdAsync(Arg.Any<CatalogItemId>(), Arg.Any<CancellationToken>()).Returns(item);
        _persistence
            .GetCatalogItemByGtinAsync("012345678901", Arg.Any<CancellationToken>())
            .Returns(item);

        var result = await _handler.Handle(ValidCommand(item.Id.Value, "012345678901"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _persistence.Received(1).UpdateCatalogItemAsync(item, Arg.Any<CancellationToken>());
    }
}

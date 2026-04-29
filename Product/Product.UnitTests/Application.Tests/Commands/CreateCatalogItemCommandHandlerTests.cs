using Application.Commands.CreateCatalogItem;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Commands;

[TestFixture]
public sealed class CreateCatalogItemCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private CreateCatalogItemCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new CreateCatalogItemCommandHandler(_persistence);
    }

    private static CreateCatalogItemCommand ValidCommand(string? gtin = null) =>
        new("Sony A7 IV", "Full-frame camera", Guid.NewGuid(), gtin, [], []);

    [Test]
    public async Task Handle_ShouldCreateCatalogItem_WhenNoGtinConflict()
    {
        _persistence
            .GetCatalogItemByGtinAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CatalogItem?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.EqualTo(Guid.Empty));
        await _persistence.Received(1).CreateCatalogItemAsync(Arg.Any<CatalogItem>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithGtin_ShouldCreateCatalogItem_WhenGtinNotTaken()
    {
        _persistence
            .GetCatalogItemByGtinAsync("012345678901", Arg.Any<CancellationToken>())
            .Returns((CatalogItem?)null);

        var result = await _handler.Handle(ValidCommand("012345678901"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _persistence.Received(1).CreateCatalogItemAsync(Arg.Any<CatalogItem>(), Arg.Any<CancellationToken>());
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
        await _persistence.DidNotReceive().CreateCatalogItemAsync(Arg.Any<CatalogItem>(), Arg.Any<CancellationToken>());
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
    public async Task Handle_ShouldMapAttributes_WhenProvided()
    {
        var command = new CreateCatalogItemCommand(
            "Sony A7 IV", "Desc", Guid.NewGuid(), null,
            [new ProductAttributeDto("Color", "Black"), new ProductAttributeDto("Weight", "659g")], []);

        CatalogItem? created = null;
        await _persistence.CreateCatalogItemAsync(
            Arg.Do<CatalogItem>(ci => created = ci), Arg.Any<CancellationToken>());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(created!.Attributes, Has.Count.EqualTo(2));
    }
}

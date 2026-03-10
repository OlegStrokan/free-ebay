using Application.Commands.ActivateProduct;
using Application.Commands.DeactivateProduct;
using Application.Commands.DeleteProduct;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

[TestFixture]
public class ActivateProductCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private ActivateProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new ActivateProductCommandHandler(_persistence);
    }

    private static Product CreateDraftProduct()
    {
        var p = Product.Create(
            SellerId.CreateUnique(), "Product", "Desc",
            CategoryId.CreateUnique(), Money.Create(10m, "USD"), 5, [], []);
        p.ClearDomainEvents();
        return p;
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_AndActivateProduct()
    {
        var product = CreateDraftProduct();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new ActivateProductCommand(product.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(product.Status, Is.EqualTo(ProductStatus.Active));
        await _persistence.Received(1).UpdateProductAsync(product, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new ActivateProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
    }

    [Test]
    public async Task Handle_AlreadyActiveProduct_ShouldReturnFailure()
    {
        var product = CreateDraftProduct();
        product.Activate();
        product.ClearDomainEvents();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new ActivateProductCommand(product.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Cannot transition"));
    }
}

[TestFixture]
public class DeactivateProductCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private DeactivateProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new DeactivateProductCommandHandler(_persistence);
    }

    private static Product CreateActiveProduct()
    {
        var p = Product.Create(
            SellerId.CreateUnique(), "Product", "Desc",
            CategoryId.CreateUnique(), Money.Create(10m, "USD"), 5, [], []);
        p.Activate();
        p.ClearDomainEvents();
        return p;
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_AndDeactivateProduct()
    {
        var product = CreateActiveProduct();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new DeactivateProductCommand(product.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(product.Status, Is.EqualTo(ProductStatus.Inactive));
        await _persistence.Received(1).UpdateProductAsync(product, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new DeactivateProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
    }

    [Test]
    public async Task Handle_DraftProduct_ShouldReturnFailure()
    {
        var p = Product.Create(
            SellerId.CreateUnique(), "Product", "Desc",
            CategoryId.CreateUnique(), Money.Create(10m, "USD"), 5, [], []);
        p.ClearDomainEvents();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(p);

        var result = await _handler.Handle(new DeactivateProductCommand(p.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Cannot transition"));
    }
}

[TestFixture]
public class DeleteProductCommandHandlerTests
{
    private IProductPersistenceService _persistence = null!;
    private DeleteProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _persistence = Substitute.For<IProductPersistenceService>();
        _handler = new DeleteProductCommandHandler(_persistence);
    }

    private static Product CreateActiveProduct()
    {
        var p = Product.Create(
            SellerId.CreateUnique(), "Product", "Desc",
            CategoryId.CreateUnique(), Money.Create(10m, "USD"), 5, [], []);
        p.Activate();
        p.ClearDomainEvents();
        return p;
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_AndDeleteProduct()
    {
        var product = CreateActiveProduct();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new DeleteProductCommand(product.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(product.Status, Is.EqualTo(ProductStatus.Deleted));
        await _persistence.Received(1).UpdateProductAsync(product, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("was not found"));
    }

    [Test]
    public async Task Handle_AlreadyDeletedProduct_ShouldReturnFailure()
    {
        var product = CreateActiveProduct();
        product.Delete();
        product.ClearDomainEvents();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new DeleteProductCommand(product.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Cannot transition"));
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenPersistenceThrows()
    {
        var product = CreateActiveProduct();
        _persistence.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);
        _persistence
            .UpdateProductAsync(product, Arg.Any<CancellationToken>())
            .Throws(new Exception("DB error"));

        var result = await _handler.Handle(new DeleteProductCommand(product.Id.Value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
    }
}

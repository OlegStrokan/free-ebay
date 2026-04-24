using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetProduct;
using Application.Queries.GetProductPrices;
using Application.Queries.GetProducts;
using Application.Queries.GetSellerProducts;
using Domain.Exceptions;
using NSubstitute;

namespace Application.Tests.Queries;

[TestFixture]
public class GetProductQueryHandlerTests
{
    private IProductReadRepository _readRepo = null!;
    private GetProductQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IProductReadRepository>();
        _handler = new GetProductQueryHandler(_readRepo);
    }

    private static ProductDetailDto SampleDto(Guid id) => new(
        id, "Name", "Desc", Guid.NewGuid(), "Category",
        99m, "USD", 5, "Active", Guid.NewGuid(), [], [], DateTime.UtcNow, null);

    [Test]
    public async Task Handle_ShouldReturnProduct_WhenFound()
    {
        var id = Guid.NewGuid();
        _readRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(SampleDto(id));

        var result = await _handler.Handle(new GetProductQuery(id), CancellationToken.None);

        Assert.That(result.ProductId, Is.EqualTo(id));
    }

    [Test]
    public void Handle_ShouldThrowProductNotFoundException_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _readRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ProductDetailDto?)null);

        Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _handler.Handle(new GetProductQuery(id), CancellationToken.None));
    }
}

[TestFixture]
public class GetProductsQueryHandlerTests
{
    private IProductReadRepository _readRepo = null!;
    private GetProductsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IProductReadRepository>();
        _handler = new GetProductsQueryHandler(_readRepo);
    }

    private static ProductDetailDto SampleDto(Guid id) => new(
        id, "Name", "Desc", Guid.NewGuid(), "Category",
        99m, "USD", 5, "Active", Guid.NewGuid(), [], [], DateTime.UtcNow, null);

    [Test]
    public async Task Handle_ShouldReturnSuccessWithAllProducts()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var dtos = ids.Select(SampleDto).ToList();
        _readRepo.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(dtos);

        var result = await _handler.Handle(new GetProductsQuery(ids), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Handle_WithEmptyList_ShouldReturnSuccessWithEmptyResult()
    {
        _readRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetProductsQuery([]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
}

[TestFixture]
public class GetProductPricesQueryHandlerTests
{
    private IProductReadRepository _readRepo = null!;
    private GetProductPricesQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IProductReadRepository>();
        _handler = new GetProductPricesQueryHandler(_readRepo);
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_WithPrices()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var prices = ids.Select(id => new ProductPriceDto(id, 49.99m, "USD")).ToList();
        _readRepo.GetPricesByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(prices);

        var result = await _handler.Handle(new GetProductPricesQuery(ids), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].Currency, Is.EqualTo("USD"));
    }

    [Test]
    public async Task Handle_WithEmptyIds_ShouldReturnSuccessWithEmptyList()
    {
        _readRepo.GetPricesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetProductPricesQuery([]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
}

[TestFixture]
public class GetSellerProductsQueryHandlerTests
{
    private IProductReadRepository _readRepo = null!;
    private GetSellerProductsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IProductReadRepository>();
        _handler = new GetSellerProductsQueryHandler(_readRepo);
    }

    private static ProductSummaryDto SampleSummary(Guid id) =>
        new(id, "Name", "Category", 99m, "USD", 5, "Active");

    [Test]
    public async Task Handle_ShouldReturnPagedResult()
    {
        var sellerId = Guid.NewGuid();
        var items = Enumerable.Range(0, 3).Select(_ => SampleSummary(Guid.NewGuid())).ToList();
        var paged = new PagedResult<ProductSummaryDto>(items, 10, 1, 3);
        _readRepo
            .GetBySellerAsync(sellerId, 1, 3, Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _handler.Handle(new GetSellerProductsQuery(sellerId, 1, 3), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Items, Has.Count.EqualTo(3));
        Assert.That(result.Value.TotalCount, Is.EqualTo(10));
        Assert.That(result.Value.Page, Is.EqualTo(1));
        Assert.That(result.Value.Size, Is.EqualTo(3));
    }

    [Test]
    public async Task Handle_WithNoProducts_ShouldReturnEmptyPage()
    {
        var sellerId = Guid.NewGuid();
        _readRepo
            .GetBySellerAsync(sellerId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductSummaryDto>([], 0, 1, 10));

        var result = await _handler.Handle(new GetSellerProductsQuery(sellerId, 1, 10), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Items, Is.Empty);
        Assert.That(result.Value.TotalCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Handle_ShouldPassCorrectPaginationToRepository()
    {
        var sellerId = Guid.NewGuid();
        _readRepo
            .GetBySellerAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductSummaryDto>([], 0, 2, 5));

        await _handler.Handle(new GetSellerProductsQuery(sellerId, 2, 5), CancellationToken.None);

        await _readRepo.Received(1).GetBySellerAsync(sellerId, 2, 5, Arg.Any<CancellationToken>());
    }
}

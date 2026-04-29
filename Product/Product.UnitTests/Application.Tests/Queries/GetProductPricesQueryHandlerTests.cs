using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetProductPrices;
using NSubstitute;

namespace Application.Tests.Queries;

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
    public async Task Handle_ShouldReturnPrices_WhenProductsFound()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var prices = ids
            .Select(id => new ProductPriceDto(id, 49.99m, "USD", Guid.Empty, Guid.NewGuid()))
            .ToList();
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

    [Test]
    public async Task Handle_WithPartialMatch_ShouldReturnOnlyFoundPrices()
    {
        var foundId = Guid.NewGuid();
        _readRepo
            .GetPricesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new ProductPriceDto(foundId, 29.99m, "EUR", Guid.Empty, Guid.NewGuid())]);

        var result = await _handler.Handle(
            new GetProductPricesQuery([foundId, Guid.NewGuid()]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].ProductId, Is.EqualTo(foundId));
    }
}

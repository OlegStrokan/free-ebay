using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetListingPrices;
using NSubstitute;

namespace Application.Tests.Queries;

[TestFixture]
public class GetListingPricesQueryHandlerTests
{
    private IListingReadRepository _readRepo = null!;
    private GetListingPricesQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IListingReadRepository>();
        _handler = new GetListingPricesQueryHandler(_readRepo);
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_WithPrices()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var prices = ids.Select(id => new ProductPriceDto(id, 49.99m, "USD", Guid.NewGuid(), Guid.NewGuid())).ToList();
        _readRepo.GetPricesByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(prices);

        var result = await _handler.Handle(new GetListingPricesQuery(ids), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].Currency, Is.EqualTo("USD"));
    }

    [Test]
    public async Task Handle_WithEmptyIds_ShouldReturnSuccessWithEmptyList()
    {
        _readRepo.GetPricesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetListingPricesQuery([]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
}

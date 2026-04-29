using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetProducts;
using NSubstitute;

namespace Application.Tests.Queries;

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
        99m, "USD", 5, "Active", Guid.NewGuid(), [], [], DateTime.UtcNow, null,
        Guid.Empty, null, "New", null);

    [Test]
    public async Task Handle_ShouldReturnAllFoundProducts()
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

    [Test]
    public async Task Handle_WithPartialMatch_ShouldReturnOnlyFoundProducts()
    {
        var foundId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        _readRepo
            .GetByIdsAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { foundId, missingId })),
                Arg.Any<CancellationToken>())
            .Returns([SampleDto(foundId)]);

        var result = await _handler.Handle(
            new GetProductsQuery([foundId, missingId]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].ProductId, Is.EqualTo(foundId));
    }
}

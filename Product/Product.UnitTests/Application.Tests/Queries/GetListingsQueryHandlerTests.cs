using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetListings;
using NSubstitute;

namespace Application.Tests.Queries;

[TestFixture]
public class GetListingsQueryHandlerTests
{
    private IListingReadRepository _readRepo = null!;
    private GetListingsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IListingReadRepository>();
        _handler = new GetListingsQueryHandler(_readRepo);
    }

    private static ProductDetailDto SampleDto(Guid id) => new(
        id, "Name", "Desc", Guid.NewGuid(), "Category",
        99m, "USD", 5, "Active", Guid.NewGuid(), [], [], DateTime.UtcNow, null,
        Guid.NewGuid(), null, "New", null);

    [Test]
    public async Task Handle_ShouldReturnSuccessWithAllListings()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var dtos = ids.Select(SampleDto).ToList();
        _readRepo.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(dtos);

        var result = await _handler.Handle(new GetListingsQuery(ids), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Handle_WithEmptyList_ShouldReturnSuccessWithEmptyResult()
    {
        _readRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetListingsQuery([]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
}

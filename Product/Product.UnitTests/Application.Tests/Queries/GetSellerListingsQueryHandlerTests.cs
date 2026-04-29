using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetSellerListings;
using NSubstitute;

namespace Application.Tests.Queries;

[TestFixture]
public class GetSellerListingsQueryHandlerTests
{
    private IListingReadRepository _readRepo = null!;
    private GetSellerListingsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IListingReadRepository>();
        _handler = new GetSellerListingsQueryHandler(_readRepo);
    }

    private static ProductSummaryDto SampleSummary(Guid id) =>
        new(id, "Name", "Category", 99m, "USD", 5, "Active", Guid.NewGuid(), Guid.NewGuid(), "New");

    [Test]
    public async Task Handle_ShouldReturnPagedResult()
    {
        var sellerId = Guid.NewGuid();
        var items = Enumerable.Range(0, 3).Select(_ => SampleSummary(Guid.NewGuid())).ToList();
        var paged = new PagedResult<ProductSummaryDto>(items, 10, 1, 3);
        _readRepo
            .GetBySellerAsync(sellerId, 1, 3, Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _handler.Handle(new GetSellerListingsQuery(sellerId, 1, 3), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Items, Has.Count.EqualTo(3));
        Assert.That(result.Value.TotalCount, Is.EqualTo(10));
        Assert.That(result.Value.Page, Is.EqualTo(1));
        Assert.That(result.Value.Size, Is.EqualTo(3));
    }

    [Test]
    public async Task Handle_WithNoListings_ShouldReturnEmptyPage()
    {
        var sellerId = Guid.NewGuid();
        _readRepo
            .GetBySellerAsync(sellerId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductSummaryDto>([], 0, 1, 10));

        var result = await _handler.Handle(new GetSellerListingsQuery(sellerId, 1, 10), CancellationToken.None);

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

        await _handler.Handle(new GetSellerListingsQuery(sellerId, 2, 5), CancellationToken.None);

        await _readRepo.Received(1).GetBySellerAsync(sellerId, 2, 5, Arg.Any<CancellationToken>());
    }
}

using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetListing;
using Domain.Exceptions;
using NSubstitute;

namespace Application.Tests.Queries;

[TestFixture]
public class GetListingQueryHandlerTests
{
    private IListingReadRepository _readRepo = null!;
    private GetListingQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _readRepo = Substitute.For<IListingReadRepository>();
        _handler = new GetListingQueryHandler(_readRepo);
    }

    private static ProductDetailDto SampleDto(Guid id) => new(
        id, "Name", "Desc", Guid.NewGuid(), "Category",
        99m, "USD", 5, "Active", Guid.NewGuid(), [], [], DateTime.UtcNow, null,
        Guid.NewGuid(), null, "New", null);

    [Test]
    public async Task Handle_ShouldReturnListing_WhenFound()
    {
        var id = Guid.NewGuid();
        _readRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(SampleDto(id));

        var result = await _handler.Handle(new GetListingQuery(id), CancellationToken.None);

        Assert.That(result.ProductId, Is.EqualTo(id));
    }

    [Test]
    public void Handle_ShouldThrowProductNotFoundException_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _readRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ProductDetailDto?)null);

        Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _handler.Handle(new GetListingQuery(id), CancellationToken.None));
    }
}

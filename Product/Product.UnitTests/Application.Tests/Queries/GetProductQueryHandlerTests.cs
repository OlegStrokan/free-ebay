using Application.DTOs;
using Application.Interfaces;
using Application.Queries.GetProduct;
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
        99m, "USD", 5, "Active", Guid.NewGuid(), [], [], DateTime.UtcNow, null,
        Guid.Empty, null, "New", null);

    [Test]
    public async Task Handle_ShouldReturnProduct_WhenFound()
    {
        var id = Guid.NewGuid();
        _readRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(SampleDto(id));

        var result = await _handler.Handle(new GetProductQuery(id), CancellationToken.None);

        Assert.That(result.ProductId, Is.EqualTo(id));
        Assert.That(result.Name, Is.EqualTo("Name"));
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

using Application.Gateways;
using Application.Queries.GetSimilarItems;
using NSubstitute;

namespace Application.Tests.Queries;

[TestFixture]
public sealed class GetSimilarItemsQueryHandlerTests
{
    private IAiSimilarItemsGateway _gateway = null!;
    private GetSimilarItemsQueryHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _gateway = Substitute.For<IAiSimilarItemsGateway>();
        _sut = new GetSimilarItemsQueryHandler(_gateway);
    }

    [Test]
    public async Task HandleAsync_ShouldCallGateway_WithCorrectParameters()
    {
        var query = new GetSimilarItemsQuery("item-123", Limit: 10, Category: "Cameras", Condition: "New");

        _gateway
            .GetSimilarItemsAsync("item-123", 10, "Cameras", "New", Arg.Any<CancellationToken>())
            .Returns(new SimilarItemsResult([]));

        await _sut.HandleAsync(query, CancellationToken.None);

        await _gateway.Received(1).GetSimilarItemsAsync(
            "item-123", 10, "Cameras", "New", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldMapGatewayResults_ToDto()
    {
        var query = new GetSimilarItemsQuery("item-456", Limit: 5, Category: null, Condition: null);

        var gatewayResult = new SimilarItemsResult(
        [
            new SimilarItemResult("similar-1", 0.92),
            new SimilarItemResult("similar-2", 0.85),
        ]);

        _gateway
            .GetSimilarItemsAsync("item-456", 5, null, null, Arg.Any<CancellationToken>())
            .Returns(gatewayResult);

        var result = await _sut.HandleAsync(query, CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items[0].CatalogItemId, Is.EqualTo("similar-1"));
        Assert.That(result.Items[0].Score, Is.EqualTo(0.92).Within(0.0001));
        Assert.That(result.Items[1].CatalogItemId, Is.EqualTo("similar-2"));
        Assert.That(result.Items[1].Score, Is.EqualTo(0.85).Within(0.0001));
    }

    [Test]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenGatewayReturnsEmpty()
    {
        var query = new GetSimilarItemsQuery("item-789", Limit: 10, Category: null, Condition: null);

        _gateway
            .GetSimilarItemsAsync("item-789", 10, null, null, Arg.Any<CancellationToken>())
            .Returns(new SimilarItemsResult([]));

        var result = await _sut.HandleAsync(query, CancellationToken.None);

        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_ShouldPassNullCategoryAndCondition_WhenNotProvided()
    {
        var query = new GetSimilarItemsQuery("item-abc", Limit: 8, Category: null, Condition: null);

        _gateway
            .GetSimilarItemsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new SimilarItemsResult([]));

        await _sut.HandleAsync(query, CancellationToken.None);

        await _gateway.Received(1).GetSimilarItemsAsync(
            "item-abc", 8, null, null, Arg.Any<CancellationToken>());
    }
}

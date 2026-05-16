using Application.Gateways;
using Application.Queries.GetFrequentlyBoughtTogether;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Queries;

[TestFixture]
public sealed class GetFrequentlyBoughtTogetherQueryHandlerTests
{
    private IAiFrequentlyBoughtTogetherGateway _gateway = null!;
    private GetFrequentlyBoughtTogetherQueryHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _gateway = Substitute.For<IAiFrequentlyBoughtTogetherGateway>();
        _sut = new GetFrequentlyBoughtTogetherQueryHandler(_gateway);
    }

    [Test]
    public async Task HandleAsync_ShouldCallGateway_WithCorrectParameters()
    {
        var query = new GetFrequentlyBoughtTogetherQuery("item-123", Limit: 10);

        _gateway
            .GetFrequentlyBoughtTogetherAsync("item-123", 10, Arg.Any<CancellationToken>())
            .Returns(new FrequentlyBoughtTogetherResult([]));

        await _sut.HandleAsync(query, CancellationToken.None);

        await _gateway.Received(1).GetFrequentlyBoughtTogetherAsync(
            "item-123", 10, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldMapGatewayResults_ToDto()
    {
        var query = new GetFrequentlyBoughtTogetherQuery("item-456", Limit: 5);

        var gatewayResult = new FrequentlyBoughtTogetherResult(
        [
            new FrequentlyBoughtTogetherItemResult("related-1", 12.0),
            new FrequentlyBoughtTogetherItemResult("related-2", 7.0),
        ]);

        _gateway
            .GetFrequentlyBoughtTogetherAsync("item-456", 5, Arg.Any<CancellationToken>())
            .Returns(gatewayResult);

        var result = await _sut.HandleAsync(query, CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items[0].CatalogItemId, Is.EqualTo("related-1"));
        Assert.That(result.Items[0].Score, Is.EqualTo(12.0).Within(0.0001));
        Assert.That(result.Items[1].CatalogItemId, Is.EqualTo("related-2"));
    }

    [Test]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenGatewayReturnsEmpty()
    {
        var query = new GetFrequentlyBoughtTogetherQuery("item-789", Limit: 10);

        _gateway
            .GetFrequentlyBoughtTogetherAsync("item-789", 10, Arg.Any<CancellationToken>())
            .Returns(new FrequentlyBoughtTogetherResult([]));

        var result = await _sut.HandleAsync(query, CancellationToken.None);

        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public void HandleAsync_WhenGatewayThrows_ShouldPropagateException()
    {
        var query = new GetFrequentlyBoughtTogetherQuery("item-999", Limit: 10);

        _gateway
            .GetFrequentlyBoughtTogetherAsync("item-999", 10, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unavailable, "service down")));

        Assert.ThrowsAsync<RpcException>(() =>
            _sut.HandleAsync(query, CancellationToken.None));
    }
}

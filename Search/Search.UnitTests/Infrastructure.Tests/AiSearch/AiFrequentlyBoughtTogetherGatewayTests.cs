using Application.Gateways;
using Grpc.Core;
using Infrastructure.AiSearch;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.AiSearch;

namespace Infrastructure.Tests.AiSearch;

[TestFixture]
public sealed class AiFrequentlyBoughtTogetherGatewayTests
{
    private AiSearchService.AiSearchServiceClient _grpcClient = null!;
    private ILogger<AiFrequentlyBoughtTogetherGateway> _logger = null!;
    private AiFrequentlyBoughtTogetherGateway _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _grpcClient = Substitute.For<AiSearchService.AiSearchServiceClient>();
        _logger = Substitute.For<ILogger<AiFrequentlyBoughtTogetherGateway>>();
        _sut = new AiFrequentlyBoughtTogetherGateway(_grpcClient, _logger);
    }

    [Test]
    public async Task GetFrequentlyBoughtTogetherAsync_ShouldMapRequestAndResponse()
    {
        var response = new AiGetFrequentlyBoughtTogetherResponse();
        response.Items.Add(new AiCoOccurrenceItem { CatalogItemId = "item-B", Score = 5.0 });
        response.Items.Add(new AiCoOccurrenceItem { CatalogItemId = "item-C", Score = 3.0 });

        _grpcClient
            .GetFrequentlyBoughtTogetherAsync(
                Arg.Any<AiGetFrequentlyBoughtTogetherRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AsyncUnaryCall<AiGetFrequentlyBoughtTogetherResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        var result = await _sut.GetFrequentlyBoughtTogetherAsync("item-A", 10, CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items[0].CatalogItemId, Is.EqualTo("item-B"));
        Assert.That(result.Items[0].Score, Is.EqualTo(5.0).Within(0.0001));
        Assert.That(result.Items[1].CatalogItemId, Is.EqualTo("item-C"));
        Assert.That(result.Items[1].Score, Is.EqualTo(3.0).Within(0.0001));
    }

    [Test]
    public async Task GetFrequentlyBoughtTogetherAsync_ShouldReturnEmptyList_WhenNoItems()
    {
        var response = new AiGetFrequentlyBoughtTogetherResponse();

        _grpcClient
            .GetFrequentlyBoughtTogetherAsync(
                Arg.Any<AiGetFrequentlyBoughtTogetherRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AsyncUnaryCall<AiGetFrequentlyBoughtTogetherResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        var result = await _sut.GetFrequentlyBoughtTogetherAsync("item-X", 5, CancellationToken.None);

        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public async Task GetFrequentlyBoughtTogetherAsync_ShouldPassCorrectParameters()
    {
        var response = new AiGetFrequentlyBoughtTogetherResponse();
        AiGetFrequentlyBoughtTogetherRequest? capturedRequest = null;

        _grpcClient
            .GetFrequentlyBoughtTogetherAsync(
                Arg.Do<AiGetFrequentlyBoughtTogetherRequest>(r => capturedRequest = r),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AsyncUnaryCall<AiGetFrequentlyBoughtTogetherResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        await _sut.GetFrequentlyBoughtTogetherAsync("catalog-123", 25, CancellationToken.None);

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.CatalogItemId, Is.EqualTo("catalog-123"));
        Assert.That(capturedRequest.Limit, Is.EqualTo(25));
    }

    [Test]
    public void GetFrequentlyBoughtTogetherAsync_WhenGrpcFails_ShouldPropagateRpcException()
    {
        _grpcClient
            .GetFrequentlyBoughtTogetherAsync(
                Arg.Any<AiGetFrequentlyBoughtTogetherRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AsyncUnaryCall<AiGetFrequentlyBoughtTogetherResponse>(
                Task.FromException<AiGetFrequentlyBoughtTogetherResponse>(
                    new RpcException(new Status(StatusCode.Unavailable, "AI service down"))),
                Task.FromResult(new Metadata()),
                () => new Status(StatusCode.Unavailable, "AI service down"),
                () => new Metadata(),
                () => { }));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetFrequentlyBoughtTogetherAsync("item-1", 10, CancellationToken.None));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Unavailable));
    }
}

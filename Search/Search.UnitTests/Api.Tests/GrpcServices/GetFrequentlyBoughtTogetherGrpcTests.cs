using Api.GrpcServices;
using Application.Gateways;
using Application.Queries.GetFrequentlyBoughtTogether;
using Application.Queries.GetSimilarItems;
using Application.Queries.SearchProducts;
using Domain.Common.Interfaces;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protos.Search;

namespace Api.Tests.GrpcServices;

[TestFixture]
public sealed class GetFrequentlyBoughtTogetherGrpcTests
{
    private IQueryHandler<SearchProductsQuery, SearchProductsResult> _searchHandler = null!;
    private IQueryHandler<GetSimilarItemsQuery, GetSimilarItemsResult> _similarHandler = null!;
    private IQueryHandler<GetFrequentlyBoughtTogetherQuery, GetFrequentlyBoughtTogetherResult> _fbtHandler = null!;
    private IAiSearchStreamGateway _streamGateway = null!;
    private ILogger<SearchGrpcService> _logger = null!;
    private ServerCallContext _callContext = null!;

    [SetUp]
    public void SetUp()
    {
        _searchHandler = Substitute.For<IQueryHandler<SearchProductsQuery, SearchProductsResult>>();
        _similarHandler = Substitute.For<IQueryHandler<GetSimilarItemsQuery, GetSimilarItemsResult>>();
        _fbtHandler = Substitute.For<IQueryHandler<GetFrequentlyBoughtTogetherQuery, GetFrequentlyBoughtTogetherResult>>();
        _streamGateway = Substitute.For<IAiSearchStreamGateway>();
        _logger = Substitute.For<ILogger<SearchGrpcService>>();
        _callContext = Substitute.For<ServerCallContext>();
        _callContext.CancellationToken.Returns(CancellationToken.None);
    }

    private SearchGrpcService BuildService() => new(_searchHandler, _similarHandler, _fbtHandler, _streamGateway, _logger);

    [Test]
    public void GetFrequentlyBoughtTogether_WhenCatalogItemIdIsEmpty_ShouldThrowInvalidArgument()
    {
        var request = new GetFrequentlyBoughtTogetherRequest { CatalogItemId = "", Limit = 10 };

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetFrequentlyBoughtTogether(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("catalog_item_id is required"));
    }

    [Test]
    public void GetFrequentlyBoughtTogether_WhenCatalogItemIdIsWhitespace_ShouldThrowInvalidArgument()
    {
        var request = new GetFrequentlyBoughtTogetherRequest { CatalogItemId = "   ", Limit = 10 };

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetFrequentlyBoughtTogether(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
    }

    [Test]
    public async Task GetFrequentlyBoughtTogether_ShouldDefaultLimit_WhenZero()
    {
        var request = new GetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = "item-123",
            Limit = 0
        };

        _fbtHandler
            .HandleAsync(Arg.Any<GetFrequentlyBoughtTogetherQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetFrequentlyBoughtTogetherResult([]));

        await BuildService().GetFrequentlyBoughtTogether(request, _callContext);

        await _fbtHandler.Received(1).HandleAsync(
            Arg.Is<GetFrequentlyBoughtTogetherQuery>(q =>
                q.CatalogItemId == "item-123" &&
                q.Limit == 10),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetFrequentlyBoughtTogether_ShouldClampLimit_WhenOver50()
    {
        var request = new GetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = "item-123",
            Limit = 100
        };

        _fbtHandler
            .HandleAsync(Arg.Any<GetFrequentlyBoughtTogetherQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetFrequentlyBoughtTogetherResult([]));

        await BuildService().GetFrequentlyBoughtTogether(request, _callContext);

        await _fbtHandler.Received(1).HandleAsync(
            Arg.Is<GetFrequentlyBoughtTogetherQuery>(q => q.Limit == 10),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetFrequentlyBoughtTogether_ShouldMapResponse_WhenItemsReturned()
    {
        var request = new GetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = "source-item",
            Limit = 5
        };

        var result = new GetFrequentlyBoughtTogetherResult(
        [
            new CoOccurrenceItemDto("related-1", 15.0),
            new CoOccurrenceItemDto("related-2", 8.0),
            new CoOccurrenceItemDto("related-3", 3.0)
        ]);

        _fbtHandler
            .HandleAsync(Arg.Any<GetFrequentlyBoughtTogetherQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await BuildService().GetFrequentlyBoughtTogether(request, _callContext);

        Assert.That(response.Items, Has.Count.EqualTo(3));
        Assert.That(response.Items[0].CatalogItemId, Is.EqualTo("related-1"));
        Assert.That(response.Items[0].Score, Is.EqualTo(15.0).Within(0.0001));
        Assert.That(response.Items[1].CatalogItemId, Is.EqualTo("related-2"));
        Assert.That(response.Items[2].CatalogItemId, Is.EqualTo("related-3"));
    }

    [Test]
    public async Task GetFrequentlyBoughtTogether_ShouldReturnEmpty_WhenNoCoOccurrences()
    {
        var request = new GetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = "lonely-item",
            Limit = 10
        };

        _fbtHandler
            .HandleAsync(Arg.Any<GetFrequentlyBoughtTogetherQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetFrequentlyBoughtTogetherResult([]));

        var response = await BuildService().GetFrequentlyBoughtTogether(request, _callContext);

        Assert.That(response.Items, Is.Empty);
    }

    [Test]
    public async Task GetFrequentlyBoughtTogether_ShouldPassValidLimit_Unchanged()
    {
        var request = new GetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = "item-456",
            Limit = 25
        };

        _fbtHandler
            .HandleAsync(Arg.Any<GetFrequentlyBoughtTogetherQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetFrequentlyBoughtTogetherResult([]));

        await BuildService().GetFrequentlyBoughtTogether(request, _callContext);

        await _fbtHandler.Received(1).HandleAsync(
            Arg.Is<GetFrequentlyBoughtTogetherQuery>(q =>
                q.CatalogItemId == "item-456" &&
                q.Limit == 25),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetFrequentlyBoughtTogether_ShouldClampLimit_WhenNegative()
    {
        var request = new GetFrequentlyBoughtTogetherRequest
        {
            CatalogItemId = "item-123",
            Limit = -5
        };

        _fbtHandler
            .HandleAsync(Arg.Any<GetFrequentlyBoughtTogetherQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetFrequentlyBoughtTogetherResult([]));

        await BuildService().GetFrequentlyBoughtTogether(request, _callContext);

        await _fbtHandler.Received(1).HandleAsync(
            Arg.Is<GetFrequentlyBoughtTogetherQuery>(q => q.Limit == 10),
            Arg.Any<CancellationToken>());
    }
}

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
public sealed class GetSimilarItemsGrpcTests
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
    public void GetSimilarItems_WhenCatalogItemIdIsEmpty_ShouldThrowInvalidArgument()
    {
        var request = new GetSimilarItemsRequest { CatalogItemId = "", Limit = 10 };

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetSimilarItems(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("catalog_item_id is required"));
    }

    [Test]
    public void GetSimilarItems_WhenCatalogItemIdIsWhitespace_ShouldThrowInvalidArgument()
    {
        var request = new GetSimilarItemsRequest { CatalogItemId = "   ", Limit = 10 };

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetSimilarItems(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
    }

    [Test]
    public async Task GetSimilarItems_ShouldMapRequestToQuery_WithDefaults()
    {
        var request = new GetSimilarItemsRequest
        {
            CatalogItemId = "item-123",
            Limit = 0, // should default to 10
            Category = "",
            Condition = ""
        };

        _similarHandler
            .HandleAsync(Arg.Any<GetSimilarItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetSimilarItemsResult([]));

        await BuildService().GetSimilarItems(request, _callContext);

        await _similarHandler.Received(1).HandleAsync(
            Arg.Is<GetSimilarItemsQuery>(q =>
                q.CatalogItemId == "item-123" &&
                q.Limit == 10 &&
                q.Category == null &&
                q.Condition == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSimilarItems_ShouldPassCategoryAndCondition_WhenProvided()
    {
        var request = new GetSimilarItemsRequest
        {
            CatalogItemId = "item-456",
            Limit = 5,
            Category = "Electronics",
            Condition = "New"
        };

        _similarHandler
            .HandleAsync(Arg.Any<GetSimilarItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetSimilarItemsResult([]));

        await BuildService().GetSimilarItems(request, _callContext);

        await _similarHandler.Received(1).HandleAsync(
            Arg.Is<GetSimilarItemsQuery>(q =>
                q.CatalogItemId == "item-456" &&
                q.Limit == 5 &&
                q.Category == "Electronics" &&
                q.Condition == "New"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSimilarItems_ShouldClampLimit_WhenOver50()
    {
        var request = new GetSimilarItemsRequest
        {
            CatalogItemId = "item-789",
            Limit = 100
        };

        _similarHandler
            .HandleAsync(Arg.Any<GetSimilarItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetSimilarItemsResult([]));

        await BuildService().GetSimilarItems(request, _callContext);

        await _similarHandler.Received(1).HandleAsync(
            Arg.Is<GetSimilarItemsQuery>(q => q.Limit == 10),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSimilarItems_ShouldMapResponse_WhenItemsReturned()
    {
        var request = new GetSimilarItemsRequest
        {
            CatalogItemId = "source-item",
            Limit = 5
        };

        var result = new GetSimilarItemsResult(
        [
            new SimilarItemDto("similar-1", 0.95),
            new SimilarItemDto("similar-2", 0.87),
            new SimilarItemDto("similar-3", 0.72)
        ]);

        _similarHandler
            .HandleAsync(Arg.Any<GetSimilarItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await BuildService().GetSimilarItems(request, _callContext);

        Assert.That(response.Items, Has.Count.EqualTo(3));
        Assert.That(response.Items[0].CatalogItemId, Is.EqualTo("similar-1"));
        Assert.That(response.Items[0].Score, Is.EqualTo(0.95).Within(0.0001));
        Assert.That(response.Items[1].CatalogItemId, Is.EqualTo("similar-2"));
        Assert.That(response.Items[2].CatalogItemId, Is.EqualTo("similar-3"));
    }

    [Test]
    public async Task GetSimilarItems_ShouldReturnEmptyList_WhenNoSimilarItemsFound()
    {
        var request = new GetSimilarItemsRequest
        {
            CatalogItemId = "unique-item",
            Limit = 10
        };

        _similarHandler
            .HandleAsync(Arg.Any<GetSimilarItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new GetSimilarItemsResult([]));

        var response = await BuildService().GetSimilarItems(request, _callContext);

        Assert.That(response.Items, Is.Empty);
    }
}

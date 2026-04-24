using Api.GrpcServices;
using Application.Queries.SearchProducts;
using Domain.Common.Interfaces;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protos.Search;

namespace Api.Tests.GrpcServices;

[TestFixture]
public sealed class SearchGrpcServiceTests
{
    private IQueryHandler<SearchProductsQuery, SearchProductsResult> _handler = null!;
    private ILogger<SearchGrpcService> _logger = null!;
    private ServerCallContext _callContext = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = Substitute.For<IQueryHandler<SearchProductsQuery, SearchProductsResult>>();
        _logger = Substitute.For<ILogger<SearchGrpcService>>();
        _callContext = Substitute.For<ServerCallContext>();
    }

    private SearchGrpcService BuildService() => new(_handler, _logger);

    [Test]
    public async Task Search_ShouldMapRequestAndResponse_WhenQuerySucceeds()
    {
        var request = new SearchRequest
        {
            Query = "laptop",
            UseAi = true,
            Page = 2,
            PageSize = 20
        };

        var result = new SearchProductsResult(
            Items:
            [
                new ProductSearchItem(
                    ProductId: Guid.NewGuid(),
                    Name: "Gaming Laptop",
                    Category: "Computers",
                    Price: 1299.99m,
                    Currency: "USD",
                    RelevanceScore: 3.14,
                    ImageUrls: ["https://img.test/laptop-1.png"])
            ],
            TotalCount: 47,
            Page: 2,
            Size: 20,
            WasAiSearch: true,
            ParsedQueryDebug: "intent:laptop");

        _handler
            .HandleAsync(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await BuildService().Search(request, _callContext);

        Assert.That(response.TotalCount, Is.EqualTo(47));
        Assert.That(response.Page, Is.EqualTo(2));
        Assert.That(response.Size, Is.EqualTo(20));
        Assert.That(response.WasAiSearch, Is.True);
        Assert.That(response.ParsedQueryDebug, Is.EqualTo("intent:laptop"));

        Assert.That(response.Items, Has.Count.EqualTo(1));
        Assert.That(response.Items[0].Name, Is.EqualTo("Gaming Laptop"));
        Assert.That(response.Items[0].Price, Is.EqualTo(1299.99).Within(0.000001));
        Assert.That(response.Items[0].ImageUrls, Has.Count.EqualTo(1));

        await _handler.Received(1).HandleAsync(
            Arg.Is<SearchProductsQuery>(q =>
                q.QueryText == "laptop" &&
                q.UseAi &&
                q.Page == 2 &&
                q.Size == 20),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Search_ShouldReturnEmptyParsedDebug_WhenApplicationReturnsNull()
    {
        var request = ValidRequest();
        var result = new SearchProductsResult(
            Items: [],
            TotalCount: 0,
            Page: 1,
            Size: 10,
            WasAiSearch: false,
            ParsedQueryDebug: null);

        _handler
            .HandleAsync(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await BuildService().Search(request, _callContext);

        Assert.That(response.ParsedQueryDebug, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Search_WhenQueryIsEmpty_ShouldThrowInvalidArgument()
    {
        var request = ValidRequest();
        request.Query = "   ";

        var ex = Assert.ThrowsAsync<RpcException>(() => BuildService().Search(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("Query cannot be empty"));
        _ = _handler.DidNotReceive().HandleAsync(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void Search_WhenPageIsLessThanOne_ShouldThrowInvalidArgument()
    {
        var request = ValidRequest();
        request.Page = 0;

        var ex = Assert.ThrowsAsync<RpcException>(() => BuildService().Search(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("Page must be >= 1"));
        _ = _handler.DidNotReceive().HandleAsync(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void Search_WhenPageSizeIsOutOfRange_ShouldThrowInvalidArgument()
    {
        var request = ValidRequest();
        request.PageSize = 101;

        var ex = Assert.ThrowsAsync<RpcException>(() => BuildService().Search(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("PageSize must be between 1 and 100"));
        _ = _handler.DidNotReceive().HandleAsync(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>());
    }

    private static SearchRequest ValidRequest() => new()
    {
        Query = "phone",
        UseAi = false,
        Page = 1,
        PageSize = 10
    };
}

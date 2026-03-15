using Application.Queries.SearchProducts;
using Domain.Common.Interfaces;
using Grpc.Core;
using Protos.Search;

namespace Api.GrpcServices;

public sealed class SearchGrpcService(
    IQueryHandler<SearchProductsQuery, SearchProductsResult> handler,
    ILogger<SearchGrpcService> logger)
    : SearchService.SearchServiceBase
{
    public override async Task<SearchResponse> Search(
        SearchRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Query cannot be empty."));
        }

        if (request.Query.Length > 500)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Query cannot exceed 500 characters."));
        }

        if (request.Page < 1)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Page must be >= 1."));
        }

        if (request.PageSize is < 1 or > 100)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "PageSize must be between 1 and 100."));
        }

        var query = new SearchProductsQuery(
            QueryText: request.Query,
            UseAi: request.UseAi,
            Page: request.Page,
            Size: request.PageSize);

        var result = await handler.HandleAsync(query, context.CancellationToken);

        var response = new SearchResponse
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            Size = result.Size,
            WasAiSearch = result.WasAiSearch,
            ParsedQueryDebug = result.ParsedQueryDebug ?? string.Empty
        };

        response.Items.AddRange(result.Items.Select(i =>
        {
            var item = new SearchResultItem
            {
                ProductId = i.ProductId.ToString(),
                Name = i.Name,
                Category = i.Category,
                Price = (double)i.Price,
                Currency = i.Currency,
                RelevanceScore = i.RelevanceScore
            };
            item.ImageUrls.AddRange(i.ImageUrls);
            return item;
        }));

        logger.LogDebug(
            "Search request processed for query [{Query}]. Returned {Count} items.",
            request.Query,
            result.Items.Count);

        return response;
    }
}
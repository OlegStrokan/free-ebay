using System.Text.Json;
using Gateway.Api.Contracts.Search;
using GrpcSearch = Protos.Search;

namespace Gateway.Api.Endpoints;

public static class SearchEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/search").WithTags("Search");

        group.MapGet("/", async (
            string q,
            int? page,
            int? pageSize,
            bool? useAi,
            GrpcSearch.SearchService.SearchServiceClient client) =>
        {
            var response = await client.SearchAsync(new GrpcSearch.SearchRequest
            {
                Query = q,
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
                UseAi = useAi ?? false
            });

            return Results.Ok(new SearchResponse(
                response.Items.Select(i => new SearchResultItemResponse(
                    i.ProductId,
                    i.Name,
                    i.Category,
                    i.Price,
                    i.Currency,
                    i.RelevanceScore,
                    i.ImageUrls.ToList())).ToList(),
                response.TotalCount,
                response.Page,
                response.Size,
                response.WasAiSearch,
                string.IsNullOrEmpty(response.ParsedQueryDebug) ? null : response.ParsedQueryDebug));
        });

        group.MapGet("/similar/{catalogItemId}", async (
            string catalogItemId,
            int? limit,
            string? category,
            string? condition,
            GrpcSearch.SearchService.SearchServiceClient client) =>
        {
            var response = await client.GetSimilarItemsAsync(new GrpcSearch.GetSimilarItemsRequest
            {
                CatalogItemId = catalogItemId,
                Limit = limit ?? 10,
                Category = category ?? string.Empty,
                Condition = condition ?? string.Empty
            });

            return Results.Ok(new SimilarItemsResponse(
                response.Items.Select(i => new SimilarItemResponse(
                    i.CatalogItemId,
                    i.Score)).ToList()));
        });

        group.MapGet("/stream", async (
            string q,
            int? page,
            int? pageSize,
            GrpcSearch.SearchService.SearchServiceClient client,
            HttpContext httpContext) =>
        {
            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            var ct = httpContext.RequestAborted;
            var request = new GrpcSearch.StreamSearchRequest
            {
                Query = q,
                Page = page ?? 1,
                PageSize = pageSize ?? 20
            };

            using var call = client.StreamSearch(request, cancellationToken: ct);

            await foreach (var msg in call.ResponseStream.ReadAllAsync(ct))
            {
                var phase = msg.Phase switch
                {
                    GrpcSearch.SearchPhase.SearchPhaseKeyword => "keyword",
                    GrpcSearch.SearchPhase.SearchPhaseMerged  => "merged",
                    _ => "keyword"
                };

                var sseEvent = new StreamSearchEvent(
                    phase,
                    msg.Items.Select(i => new SearchResultItemResponse(
                        i.ProductId,
                        i.Name,
                        i.Category,
                        i.Price,
                        i.Currency,
                        i.RelevanceScore,
                        i.ImageUrls.ToList())).ToList(),
                    msg.TotalCount,
                    msg.WasAiSearch);

                var json = JsonSerializer.Serialize(sseEvent, JsonOptions);
                await httpContext.Response.WriteAsync($"event: {phase}\ndata: {json}\n\n", ct);
                await httpContext.Response.Body.FlushAsync(ct);
            }
        });

        return group;
    }
}

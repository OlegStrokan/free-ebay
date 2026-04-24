using Gateway.Api.Contracts.Search;
using GrpcSearch = Protos.Search;

namespace Gateway.Api.Endpoints;

public static class SearchEndpoints
{
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

        return group;
    }
}

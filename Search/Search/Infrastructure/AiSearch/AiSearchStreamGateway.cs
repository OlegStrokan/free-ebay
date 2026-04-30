using System.Runtime.CompilerServices;
using Application.Gateways;
using Application.Queries.SearchProducts;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Protos.AiSearch;

namespace Infrastructure.AiSearch;

/// <summary>
/// Opens a bidirectional gRPC stream per request.
/// Sends one query, yields two phases (keyword then merged), closes.
/// Cancellation (client disconnect) propagates to server and kills in-flight Qdrant work.
/// </summary>
public sealed class AiSearchStreamGateway(
    AiSearchService.AiSearchServiceClient client,
    ILogger<AiSearchStreamGateway> logger) : IAiSearchStreamGateway
{
    public async IAsyncEnumerable<StreamSearchResult> SearchStreamAsync(
        string query, int page, int size,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N");

        using var call = client.SearchStream(cancellationToken: ct);

        await call.RequestStream.WriteAsync(new AiStreamSearchRequest
        {
            RequestId = requestId,
            Query = query,
            Page = page,
            PageSize = size
        }, ct);

        await call.RequestStream.CompleteAsync();

        logger.LogDebug("Opened stream for query [{Query}] request_id={RequestId}",
            query, requestId);

        await foreach (var response in call.ResponseStream.ReadAllAsync(ct))
        {
            var items = response.Items
                .Select(i => new ProductSearchItem(
                    ProductId: Guid.Parse(i.ProductId),
                    Name: i.Name,
                    Category: i.Category,
                    Price: (decimal)i.Price,
                    Currency: i.Currency,
                    RelevanceScore: i.RelevanceScore,
                    ImageUrls: i.ImageUrls.ToList()))
                .ToList();

            yield return new StreamSearchResult(
                Items: items,
                TotalCount: response.TotalCount,
                WasAiSearch: response.UsedAi,
                Phase: response.Phase switch
                {
                    AiSearchPhase.SearchPhaseKeyword => SearchResultPhase.Keyword,
                    AiSearchPhase.SearchPhaseMerged => SearchResultPhase.Merged,
                    _ => SearchResultPhase.Keyword
                }
            );
        }
    }
}

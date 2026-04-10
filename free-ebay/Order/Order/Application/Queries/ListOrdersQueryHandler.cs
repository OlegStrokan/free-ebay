using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public record ListOrdersQuery(int PageNumber, int PageSize) : IRequest<Result<List<OrderSummaryResponse>>>;

public class ListOrdersQueryHandler(
    IOrderReadRepository readRepository,
    ILogger<ListOrdersQueryHandler> logger)
    : IRequestHandler<ListOrdersQuery, Result<List<OrderSummaryResponse>>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<List<OrderSummaryResponse>>> Handle(
        ListOrdersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageSize = Math.Min(request.PageSize, MaxPageSize);
            var orders = await readRepository.GetOrderAsync(request.PageNumber, pageSize, cancellationToken);
            return Result<List<OrderSummaryResponse>>.Success(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing orders (page {Page}, size {Size})",
                request.PageNumber, request.PageSize);
            return Result<List<OrderSummaryResponse>>.Failure("Failed to list orders.");
        }
    }
}

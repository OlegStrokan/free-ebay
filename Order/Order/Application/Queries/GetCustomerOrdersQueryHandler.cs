using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public record GetCustomerOrdersQuery(Guid CustomerId) : IRequest<Result<List<OrderSummaryResponse>>>;

public class GetCustomerOrdersQueryHandler(
    IOrderReadRepository readRepository,
    ILogger<GetCustomerOrdersQueryHandler> logger)
    : IRequestHandler<GetCustomerOrdersQuery, Result<List<OrderSummaryResponse>>>
{
    public async Task<Result<List<OrderSummaryResponse>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await readRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
            return Result<List<OrderSummaryResponse>>.Success(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching orders for customer {CustomerId}", request.CustomerId);
            return Result<List<OrderSummaryResponse>>.Failure("Failed to retrieve customer orders.");
        }
    }
}

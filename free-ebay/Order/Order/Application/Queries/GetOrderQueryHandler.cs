using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public record GetOrderQuery(Guid OrderId) : IRequest<Result<OrderResponse>>;

public class GetOrderQueryHandler(
    IOrderReadRepository readRepository,
    ILogger<GetOrderQueryHandler> logger)
    : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await readRepository.GetByIdAsync(request.OrderId, cancellationToken);

            if (order is null)
                return Result<OrderResponse>.Failure($"Order {request.OrderId} not found.");

            return Result<OrderResponse>.Success(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching order {OrderId}", request.OrderId);
            return Result<OrderResponse>.Failure("Failed to retrieve order.");
        }
    }
}

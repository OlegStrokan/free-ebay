using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public record GetRecurringOrderQuery(Guid RecurringOrderId) : IRequest<Result<RecurringOrderDetail>>;

public sealed class GetRecurringOrderQueryHandler(
    IRecurringOrderReadRepository readRepository,
    ILogger<GetRecurringOrderQueryHandler> logger)
    : IRequestHandler<GetRecurringOrderQuery, Result<RecurringOrderDetail>>
{
    public async Task<Result<RecurringOrderDetail>> Handle(
        GetRecurringOrderQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await readRepository.GetByIdAsync(request.RecurringOrderId, cancellationToken);

            if (detail is null)
                return Result<RecurringOrderDetail>.Failure(
                    $"RecurringOrder {request.RecurringOrderId} not found");

            return Result<RecurringOrderDetail>.Success(detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching RecurringOrder {Id}", request.RecurringOrderId);
            return Result<RecurringOrderDetail>.Failure("Failed to retrieve recurring order.");
        }
    }
}

using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public record GetCustomerRecurringOrdersQuery(Guid CustomerId) : IRequest<Result<List<RecurringOrderSummary>>>;

public sealed class GetCustomerRecurringOrdersQueryHandler(
    IRecurringOrderReadRepository readRepository,
    ILogger<GetCustomerRecurringOrdersQueryHandler> logger)
    : IRequestHandler<GetCustomerRecurringOrdersQuery, Result<List<RecurringOrderSummary>>>
{
    public async Task<Result<List<RecurringOrderSummary>>> Handle(
        GetCustomerRecurringOrdersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var summaries = await readRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
            return Result<List<RecurringOrderSummary>>.Success(summaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching recurring orders for customer {CustomerId}", request.CustomerId);
            return Result<List<RecurringOrderSummary>>.Failure("Failed to retrieve customer recurring orders.");
        }
    }
}

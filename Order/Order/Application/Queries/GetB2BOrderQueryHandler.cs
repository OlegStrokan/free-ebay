using Application.Common;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public record GetB2BOrderQuery(Guid B2BOrderId) : IRequest<Result<B2BOrderDetail>>;

// Reads from the denormalized B2BOrderReadModel via IB2BOrderReadRepository.
// No aggregate replay needed — O(1) DB lookup instead of snapshot + delta.
public class GetB2BOrderQueryHandler(
    IB2BOrderReadRepository readRepository,
    ILogger<GetB2BOrderQueryHandler> logger)
    : IRequestHandler<GetB2BOrderQuery, Result<B2BOrderDetail>>
{
    public async Task<Result<B2BOrderDetail>> Handle(
        GetB2BOrderQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await readRepository.GetByIdAsync(request.B2BOrderId, cancellationToken);

            if (detail is null)
                return Result<B2BOrderDetail>.Failure($"B2BOrder {request.B2BOrderId} not found");

            return Result<B2BOrderDetail>.Success(detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result<B2BOrderDetail>.Failure("Failed to retrieve B2B order.");
        }
    }
}

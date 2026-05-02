using Application.Common;
using Application.Interfaces;
using Domain.Entities.Order;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

// we create normal order like in create order command handler 
// save to db, so later job worker get it
namespace Application.Commands.FinalizeQuote;

public class FinalizeQuoteCommandHandler(
    IB2BOrderPersistenceService b2bPersistenceService,
    ILogger<FinalizeQuoteCommandHandler> logger)
    : IRequestHandler<FinalizeQuoteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(FinalizeQuoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var b2bOrder = await b2bPersistenceService.LoadB2BOrderAsync(
                request.B2BOrderId, cancellationToken);

            if (b2bOrder is null)
                return Result<Guid>.Failure($"B2BOrder {request.B2BOrderId} not found");

            // Convert quote items -> standard OrderItems
            var orderItems = b2bOrder.ActiveItems
                .Select(i => OrderItem.Create(
                    i.ProductId,
                    i.Quantity,
                    i.EffectiveUnitPrice))
                .ToList();

            var order = Order.Create(b2bOrder.CustomerId, b2bOrder.DeliveryAddress, orderItems, request.PaymentMethod);

         await b2bPersistenceService.FinalizeB2BOrderAsync(
                request.B2BOrderId,
                order,
                request.IdempotencyKey,
                cancellationToken);

            logger.LogInformation(
                "Finalized B2BOrder {B2BOrderId} → Order {OrderId}",
                request.B2BOrderId, order.Id.Value);

            return Result<Guid>.Success(order.Id.Value);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain violation finalizing B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to finalize B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

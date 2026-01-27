using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class ConfirmReturnReceivedStep(
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmReturnReceivedStep> logger
    ) : ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "ConfirmReturnReceived";
    public int Order => 3;

    public async Task<StepResult> ExecuteAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Confirming return for order {OrderId}, shipment {ShipmentId}",
                data.CorrelationId,
                context.ReturnShipmentId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (order == null)
                return StepResult.Failure($"Order {data.CorrelationId} not found");

            if (order.Status != OrderStatus.ReturnRequested)
            {
                logger.LogWarning(
                    "Order {OrderId} is in status {Status}, expected ReturnRequested",
                    data.CorrelationId,
                    order.Status);


                if (order.Status == OrderStatus.ReturnReceived)
                {
                    logger.LogInformation(
                        "Order {OrderId} already marked as return Received (duplicate webhook)",
                        data.CorrelationId);

                    return StepResult.SuccessResult(new Dictionary<string, object>
                    {
                        ["Status"] = "AlreadyProcessed"
                    });
                }


                return StepResult.Failure(
                    $"Order in unexpected status: {order.Status}");
            }

            order.ConfirmReturnReceived();

            await orderRepository.AddAsync(order, cancellationToken);

            foreach (var domainEvent in order.UncommitedEvents)
            {
                await outboxRepository.AddAsync(
                    domainEvent.EventId,
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent),
                    domainEvent.OccurredOn,
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            order.MarkEventsAsCommited();

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            order.MarkEventsAsCommited();

            var receivedAt = DateTime.UtcNow;
            context.ReturnReceivedAt = receivedAt;

            logger.LogInformation(
                "Return confirmed for order {OrderId}. Package physically received at warehouse.",
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["ReturnShipmentId"] = context.ReturnShipmentId ?? "N/A",
                ["ReceivedAt"] = receivedAt,
                ["Status"] = "ReturnReceived"
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            logger.LogError(
                ex,
                "Failed to confirm return receipt for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Failed to confirm return: {ex.Message}");
        }

        
    }

    public async Task CompensateAsync(
        ReturnSagaData data,
        ReturnSagaContext context, 
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogWarning(
                "Compensating ConfirmReturnReceived for order {OrderId}",
                data.CorrelationId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (order == null)
            {
                logger.LogWarning(
                    "Order {OrderId} not found during compensation",
                    data.CorrelationId);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            if (order.Status == OrderStatus.ReturnReceived)
            {
                logger.LogInformation(
                    "Reverting return receipt for order {OrderId} (status: {Status} -> ReturnRequested)",
                    data.CorrelationId,
                    order.Status);

                order.RevertReturnReceipt();

                await orderRepository.AddAsync(order, cancellationToken);

                foreach (var domainEvent in order.UncommitedEvents)
                {
                    await outboxRepository.AddAsync(
                        domainEvent.EventId,
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent),
                        domainEvent.OccurredOn,
                        cancellationToken);
                }

                order.MarkEventsAsCommited();

                logger.LogInformation(
                    "Successfully reverted return receipt for order {OrderId}",
                    data.CorrelationId);

            }
            else
            {
                logger.LogInformation(
                    "Order {OrderId} is in status {Status}, no revert needed",
                    data.CorrelationId,
                    order.Status);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            logger.LogError(
                ex,
                "Failed to compensate ConfirmReturnReceived for order {OrderId}. Manuel intervention required!",
                data.CorrelationId);
        }
    }
}






















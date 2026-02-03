using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class ConfirmReturnReceivedStep(
    IOutboxRepository outboxRepository,
    IReturnRequestRepository returnRequestRepository,
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

            var returnRequest = await returnRequestRepository.GetByOrderIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (returnRequest == null)
                return StepResult.Failure($"ReturnRequest for order {data.CorrelationId} not found");

            if (returnRequest.Status != ReturnStatus.Received)
            {
                logger.LogWarning(
                    "ReturnRequest {ReturnRequestId} is in status {Status}, expected Pending",
                    returnRequest.Id.Value,
                    returnRequest.Status);


                if (returnRequest.Status == ReturnStatus.Received)
                {
                    logger.LogInformation(
                        "ReturnRequest {ReturnRequestId} already marked as received (duplicate webhook)",
                        returnRequest.Id.Value);

                    return StepResult.SuccessResult(new Dictionary<string, object>
                    {
                        ["Status"] = "AlreadyProcessed"
                    });
                }
                
                return StepResult.Failure($"ReturnRequest in unexpected status: {returnRequest.Status}");
            }

            returnRequest.MarkAsReceived();

            await returnRequestRepository.AddAsync(returnRequest, cancellationToken);

            foreach (var domainEvent in returnRequest.UncommitedEvents)
            {
                await outboxRepository.AddAsync(
                    domainEvent.EventId,
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent),
                    domainEvent.OccurredOn,
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken); ;
            
            returnRequest.MarkEventsAsCommited();

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

            var returnRequest = await returnRequestRepository.GetByOrderIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (returnRequest == null)
            {
                logger.LogWarning(
                    "ReturnRequest for order {OrderId} not found during compensation",
                    data.CorrelationId);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            if (returnRequest.Status == ReturnStatus.Received)
            {
                logger.LogInformation(
                    "Reverting ReturnRequest {ReturnRequestId} from Received to Pending status",
                    returnRequest.Id.Value);

                // Note: In event-sourced systems, compensation typically involves
                // adding compensating events rather than directly modifying state.
                // For this MVP, we'll log the need for manual intervention.
                
                logger.LogWarning(
                    "ReturnRequest {ReturnRequestId} was marked as received but saga failed. " +
                    "Manual review required to determine if items were actually returned.",
                    returnRequest.Id.Value);
            }
            else
            {
                logger.LogInformation(
                    "ReturnRequest {ReturnRequestId} is in status {Status}, no revert needed",
                    returnRequest.Id.Value,
                    returnRequest.Status);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to compensate ConfirmReturnReceived for order {OrderId}. Manual intervention required!",
                data.CorrelationId);
        }
    }
}






















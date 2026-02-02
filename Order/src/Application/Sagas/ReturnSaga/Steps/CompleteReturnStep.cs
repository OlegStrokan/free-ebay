using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class CompleteReturnStep(
    IReturnRequestRepository returnRequestRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<CompleteReturnStep> logger)
: ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "CompleteReturn";
    public int Order => 6;

    public async Task<StepResult> ExecuteAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Completing return process for order {OrderId}",
                data.CorrelationId);

            var returnRequest = await returnRequestRepository.GetByOrderIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (returnRequest == null)
                return StepResult.Failure($"ReturnRequest {data.CorrelationId} not found");
            
            returnRequest.Complete();

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
            await transaction.CommitAsync(cancellationToken);
            
            returnRequest.MarkEventsAsCommited();

            logger.LogInformation(
                "Return completed successfully for order {OrderId}. " +
                "Final status: Returned",
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["OrderId"] = data.CorrelationId,
                ["FinalStatus"] = "Returned",
                ["RefundId"] = context.RefundId ?? "N/A",
                ["RefundAmount"] = data.RefundAmount
            });
        }
        
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            logger.LogError(
                ex,
                "Failed to complete return for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Return completion failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        ReturnSagaData data, 
        ReturnSagaContext context, 
        CancellationToken cancellationToken)
    {
       // final step compensation
       // Revert ReturnRequest from Completed back to Refunded status
       
       logger.LogWarning(
           "Compensation triggered on CompleteReturn step for order {OrderId}. " +
           "Reverting ReturnRequest status from Completed to Refunded",
           data.CorrelationId);

       try
       {
           await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

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

           if (returnRequest.Status == ReturnStatus.Completed)
           {
               logger.LogInformation(
                   "Reverting ReturnRequest {ReturnRequestId} from Completed to Refunded status",
                   returnRequest.Id.Value);
               
               // note: ReturnRequest doesn't have a direct "revert" method since it's event-sourced
               // In a real implementation, we might need to add compensation events or 
               // recreate the aggregate with different history

               logger.LogWarning(
                   "ReturnRequest {ReturnRequestId} compensation requires manual review. " +
                   "Return marked as completed but saga failed - possible inconsistent state.",
                   returnRequest.Id.Value);
           }
           else
           {
               logger.LogInformation(
                   "ReturnRequest {ReturnRequestId} is in status {Status}, no revert needed",
                   returnRequest.Id.Value,
                   returnRequest.Status);
           }
       }
       catch (Exception ex)
       {
           logger.LogError(
               ex,
               "Failed to compensate CompleteReturn step for order {OrderId}",
               data.CorrelationId);
       }
    }
}
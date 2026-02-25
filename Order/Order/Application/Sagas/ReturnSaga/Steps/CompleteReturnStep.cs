using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class CompleteReturnStep(
    IReturnRequestPersistenceService returnRequestPersistenceService,
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

        try
        {
            logger.LogInformation(
                "Completing return process for order {OrderId}",
                data.CorrelationId);

            await returnRequestPersistenceService.UpdateReturnRequestAsync(
                data.CorrelationId,
                returnRequest =>
                {
                    returnRequest.Complete();
                    return Task.CompletedTask;
                }, cancellationToken);
            
            logger.LogInformation(
                "Return completed successfully for order {OrderId}. Final status: Returned",
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
       
       logger.LogWarning(
           "Compensation triggered on CompleteReturn step for order {OrderId}. " +
           "ReturnRequest was marked as Completed but saga failed. " +
           "Manual review required - possible inconsistent state.",
           data.CorrelationId);

       // Note: This is the final step, so compensation just means logging.
       // The ReturnRequest was marked complete, but something after failed.
       // Manual intervention is required to determine correct state.

    }
}
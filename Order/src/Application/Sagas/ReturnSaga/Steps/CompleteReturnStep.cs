using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class CompleteReturnStep(
    IOrderRepository orderRepository,
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

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (order == null)
                return StepResult.Failure($"Order {data.CorrelationId} not found");
            
            // mark order as fully returned
            order.CompleteReturn();

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

            return StepResult.Failure("$Return completion failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        ReturnSagaData data, 
        ReturnSagaContext context, 
        CancellationToken cancellationToken)
    {
       // final step
       //  @think: if we reach this point and need to compensate, the order should
       // probably be reverted to a ReturnFailed status
       
       logger.LogWarning(
           "Compensation triggered on CompleteReturn step for order {OrderId}. " +
           "This indicates a critical failure after return processing.",
           data.CorrelationId);

       try
       {
           await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

           var order = await orderRepository.GetByIdAsync(
               OrderId.From(data.CorrelationId),
               cancellationToken);

           if (order != null)
           {
               // revert to completed status
               // @think: in real world, we might have a ReturnFailed status

               logger.LogWarning(
                   "Order {OrderId} return process failed during final step. " +
                   "Manual review required.",
                   data.CorrelationId);
           }

           await transaction.CommitAsync(cancellationToken);
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
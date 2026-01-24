using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class ValidateReturnRequestStep(
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<ValidateReturnRequestStep> logger
    ) : ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "ValidateReturnRequest";
    public int Order => 1;

    public async Task<StepResult> ExecuteAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Validation return request for order {OrderId}",
                data.CorrelationId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId), cancellationToken);

            if (order == null)
                return StepResult.Failure($"Order {data.CorrelationId} not found");
            
            var itemsToReturn = data.ReturnedItems.Select(dto =>
                OrderItem.Create(
                    ProductId.From(dto.ProductId),
                    dto.Quantity,
                    Money.Create(dto.Price, dto.Currency)
                )).ToList();

            order.RequestReturn(data.ReturnReason, itemsToReturn);

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
                "Return request validated and saved for order {OrderId}",
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["OrderId"] = data.CorrelationId,
                ["RefundAmount"] = data.RefundAmount,
                ["ItemsCount"] = data.ReturnedItems.Count
            });
        }

        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            logger.LogError(
                ex,
                "Failed to validate return request for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Validation failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        // if validation fails, there's nothing to compensate, the order status would still be completed
        
        logger.LogInformation(
            "No compensation needed for ValidateReturnRequest step (order {OrderId})",
            data.CorrelationId);

        await Task.CompletedTask;
    }
}
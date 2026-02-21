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
    IReturnRequestPersistenceService returnRequestPersistenceService,
    IReturnRequestRepository returnRequestRepository,
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

        try
        {

            if (context.ReturnRequestValidated)
            {
                logger.LogInformation(
                    "Return request already validated for order {OrderId}. Skipping.",
                    data.CorrelationId);

                return StepResult.SuccessResult(new Dictionary<string, object>
                {
                    ["OrderId"] = data.CorrelationId,
                    ["Idempotent"] = true
                });
            }
            
            logger.LogInformation(
                "Validation return request for order {OrderId}",
                data.CorrelationId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId), cancellationToken);

            if (order == null)
                return StepResult.Failure($"Order {data.CorrelationId} not found");

            if (order.Status != OrderStatus.Completed)
                return StepResult.Failure(
                    $"Order {data.CorrelationId} must be completed to request return." +
                    $"Current status: {order.Status}");

            var existingRequest = await returnRequestRepository.GetByOrderIdAsync(
                OrderId.From(data.CorrelationId), cancellationToken);

            if (existingRequest != null)
            {
                logger.LogInformation("ReturnRequest already exists for Order {OrderId}. Attaching saga.",
                    data.CorrelationId);
                context.ReturnRequestValidated = true;

                return StepResult.SuccessResult(new Dictionary<string, object>
                {
                    ["OrderId"] = data.CorrelationId,
                    ["Idempotent"] = true,
                    ["Source"] = "ExistingRecord"
                });
            }

            if (!order.IsEligibleForReturn())
            {
                return StepResult.Failure($"Order {data.CorrelationId} id not eligible for return.");
            }

            var itemsToReturn = data.ReturnedItems.Select(dto =>
                OrderItem.Create(
                    ProductId.From(dto.ProductId),
                    dto.Quantity,
                    Money.Create(dto.Price, dto.Currency)
                )).ToList();

            var refundAmount = Money.Create(data.RefundAmount, data.Currency);
            var returnWindow = TimeSpan.FromDays(14); // @todo: move to domain service and shit

            
            var returnRequest = ReturnRequest.Create(
                orderId: OrderId.From(data.CorrelationId),
                customerId: CustomerId.From(data.CustomerId),
                reason: data.ReturnReason,
                itemsToReturn: itemsToReturn,
                refundAmount: refundAmount,
                orderCompletedAt: order.CompletedAt!.Value, // order must be already completed
                orderItems: order.Items.ToList(),
                returnWindow: returnWindow);

            await returnRequestPersistenceService.CreateReturnRequestAsync(
                returnRequest,
                null,
                null,
                cancellationToken);

            context.ReturnRequestValidated = true;
            
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
        logger.LogInformation(
            "No compensation needed for ValidateReturnRequest step (order {OrderId}). " +
            "ReturnRequest created successfully.",
            data.CorrelationId);

        await Task.CompletedTask;
    }
}
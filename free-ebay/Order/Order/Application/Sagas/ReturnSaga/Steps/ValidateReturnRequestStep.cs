using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Entities.Order;
using Domain.Entities.RequestReturn;
using Domain.Services;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class ValidateReturnRequestStep(
    IOrderPersistenceService orderPersistenceService,
    IReturnRequestPersistenceService returnRequestPersistenceService,
    ReturnPolicyService returnPolicyService,
    ILogger<ValidateReturnRequestStep> logger
    ) : ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "ValidateReturnRequest";
    public int Order => 1;

    public async Task<StepOutcome> ExecuteAsync(
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

                return new Completed(new Dictionary<string, object>
                {
                    ["OrderId"] = data.CorrelationId,
                });
            }
            
            logger.LogInformation(
                "Validating return request for order {OrderId}",
                data.CorrelationId);

            var order = await orderPersistenceService.LoadOrderAsync(
                data.CorrelationId, cancellationToken);

            if (order == null)
                return new Fail($"Order {data.CorrelationId} not found");

            if (order.Status != OrderStatus.Completed)
                return new Fail(
                    $"Order {data.CorrelationId} must be completed to request return." +
                    $"Current status: {order.Status}");

            var existingRequest = await returnRequestPersistenceService.LoadByOrderIdAsync(
                data.CorrelationId, cancellationToken);

            if (existingRequest != null)
            {
                logger.LogInformation("ReturnRequest already exists for Order {OrderId}. Attaching saga.",
                    data.CorrelationId);
                context.ReturnRequestValidated = true;

                return new Completed(new Dictionary<string, object>
                {
                    ["OrderId"] = data.CorrelationId,
                    ["Source"] = "ExistingRecord"
                });
            }

            if (!order.IsEligibleForReturn())
            {
                return new Fail($"Order {data.CorrelationId} id is not eligible for return.");
            }

            var itemsToReturn = data.ReturnedItems.Select(dto =>
                OrderItem.Create(
                    ProductId.From(dto.ProductId),
                    dto.Quantity,
                    Money.Create(dto.Price, dto.Currency)
                )).ToList();

            var refundAmount = Money.Create(data.RefundAmount, data.Currency);
            
            /* @todo: integrate user service
             * to make it correct i need to update user proto and 
             * take all needed info as countryCode, CustomerTier
             * in addition i need one more integration to product to get
             * list of categories which products have from current order
            */
            var policyContext = new ReturnPolicyContext(
                CountryCode: "US",
                ProductCategories: new List<string>(), 
                CustomerTier: "Standard", 
                IsHolidaySeason: false 
            );
            
            var returnWindow = returnPolicyService.CalculateReturnWindow(policyContext);

            
            var returnRequest = RequestReturn.Create(
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

            return new Completed(new Dictionary<string, object>
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

            return new Fail($"Validation failed: {ex.Message}");
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
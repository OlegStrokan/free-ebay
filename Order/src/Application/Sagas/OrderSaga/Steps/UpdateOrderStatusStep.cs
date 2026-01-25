using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga.Steps;

public class UpdateOrderStatusStep(
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateOrderStatusStep> logger
    )
    : ISagaStep<OrderSagaData, OrderSagaContext>
{
    public string StepName => "UpdateOrderStatus";
    public int Order => 3;

    public async Task<StepResult> ExecuteAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            logger.LogInformation(
                "Updating order {OrderId} status to Paid",
                data.CorrelationId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);


            if (order == null)
            {
                return StepResult.Failure($"Order {data.CorrelationId} not found");
            }

            if (string.IsNullOrEmpty(context.PaymentId))
                return StepResult.Failure("Payment ID not found in saga context");

            var paymentId = PaymentId.From(context.PaymentId);
            order.Pay(paymentId);

            await orderRepository.AddAsync(order, cancellationToken);
            
            logger.LogInformation(
                "Successfully updated order {OrderId} to Paid status and saved event to outbox",
                data.CorrelationId);

            foreach (var domainEvent in order.UncommitedEvents)
            {
                await outboxRepository.AddAsync(
                    domainEvent.EventId,
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent),
                    domainEvent.OccurredOn,
                    cancellationToken
                );
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["OrderId"] = data.CorrelationId,
                ["Status"] = "Paid"
            });
        }
        catch (Exception ex)
        {

            await transaction.RollbackAsync(cancellationToken);
            
            logger.LogError(
                ex,
                "Failed to update order {OrderId} status",
                data.CorrelationId);

            return StepResult.Failure($"Failed to update order status: {ex.Message}");
        }
    }

    public async Task CompensateAsync(OrderSagaData data, OrderSagaContext context, CancellationToken cancellationToken)
    {

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            logger.LogInformation(
                "Cancelling order {OrderId}",
                data.CorrelationId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId),
                cancellationToken);

            if (order == null)
            {
                logger.LogWarning("Order {OrderId} nto found for cancellation", data.CorrelationId);
                return;
            }

            var failureReasons = new List<string>
            {
                "Saga compensation triggered",
                "One or more saga steps failed"
            };
            
            order.Cancel(failureReasons);

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
            
            logger.LogInformation("Successfully cancelled order {OrderId}",
                data.CorrelationId);

        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to cancel order {OrderId} during compensation",
                data.CorrelationId);
        }
    }
}
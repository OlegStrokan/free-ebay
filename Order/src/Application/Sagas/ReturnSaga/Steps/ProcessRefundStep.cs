using System.Text.Json;
using Application.Gateways;
using Application.Interfaces;
using Application.Sagas.Steps;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public class ProcessRefundStep(
    IPaymentGateway paymentGateway,
    IOrderRepository orderRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProcessRefundStep> logger
    ) : ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "ProcessRefund";
    public int Order => 4;

    public async Task<StepResult> ExecuteAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Processing refund for order {OrderId}, amount {Amount} {Current}",
                data.CorrelationId,
                data.RefundAmount,
                data.Currency);

            var refundId = await paymentGateway.RefundAsync(
                // @think: in real app, store original payment id - what do you mean by that?
                paymentId: $"original-payment-{data.CorrelationId}",
                amount: data.RefundAmount,
                reason: $"Return request: {data.ReturnReason}",
                cancellationToken);

            context.RefundId = refundId;

            logger.LogInformation(
                "Refund processed successfully. Refund ID: {RefundId}",
                refundId);

            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId), cancellationToken);

            if (order == null)
            {
                return StepResult.Failure($"Order {data.CorrelationId} not found");
            }

            order.ProcessRefund(
                refundId,
                Money.Create(data.RefundAmount, data.Currency));

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
                "Refund {RefundId} processed and saved for order {OrderId}",
                refundId,
                data.CorrelationId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["RefundId"] = refundId,
                ["Amount"] = data.RefundAmount,
                ["Currency"] = data.Currency,
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            logger.LogError(
                ex,
                "Failed to process refund for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Refund processing failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(context.RefundId))
        {
            logger.LogInformation(
                "No refund to reverse for order {OrderId}",
                data.CorrelationId);
            return;
        }

        try
        {
            logger.LogWarning(
                "CRITICAL: Attempting to reverse refund {RefundId} for order {OrderId}",
                context.RefundId,
                data.CorrelationId);

            // @think: in real world reversing a refund is complex
            // we might need to re-charge the customer or mark for manual review

            logger.LogError(
                "Refund {RefundId} cannot be automatically reversed. " +
                "MANUAL INTERVENTION REQUIRED - Customer may need to be re-charged.",
                context.RefundId);

            // @think: Send alert to operation team
            // await _alertingService.SendCriticalAlert(...);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed during refund compensation for {RefundId}. " + 
                "CRITICAL: Manual review required!",
                context.RefundId);
        }
    }
}
using System.Text.Json;
using System.Xml.Schema;
using Application.Common.Enums;
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

            
            var order = await orderRepository.GetByIdAsync(
                OrderId.From(data.CorrelationId), cancellationToken);

            if (order == null)
            {
                return StepResult.Failure($"Order {data.CorrelationId} not found");
            }
            
            if (order.PaymentId == null)
                return StepResult.Failure("Order has no payment ID - cannot process refund");

            
            logger.LogInformation(
                "Processing refund for order {OrderId}, amount {Amount} {Currency}, original payment {PaymentId}",
                data.CorrelationId,
                data.RefundAmount,
                data.Currency,
                order.PaymentId);
            
            var refundId = await paymentGateway.RefundAsync(
                paymentId: order.PaymentId,
                amount: data.RefundAmount,
                reason: $"Return request: {data.ReturnReason}",
                cancellationToken);

            context.RefundId = refundId;

            logger.LogInformation(
                "Refund processed successfully. Refund ID: {RefundId}",
                refundId);


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
                ["OriginalPaymentId"] = order.PaymentId.ToString(),
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
            logger.LogCritical(
                "CRITICAL: Refund compensation triggered for order {OrderId}, refund {RefundId}. " +
                "Refund has been issued to customer but return saga failed. " +
                "Customer has money AND potentially keeping the product. " +
                "IMMEDIATE ACTION REQUIRED: Review order and determine if re-charge is needed.",
                data.CorrelationId,
                context.RefundId);

            await SendCriticalAlertAsync(
                alertType: "RefundCompensationRequired",
                orderId: data.CorrelationId,
                refundId: context.RefundId,
                message: "Refund issued but return saga failed - manual review required",
                severity: AlertSeverity.Critical,
                cancellationToken);

            await CreateManualInterventionTicketAsync(
                orderId: data.CorrelationId,
                refundId: context.RefundId,
                issue: "Refund issued but downstream steps failed",
                suggestedAction: "1. Verify if customer returned items\n" +
                                 "2. If items not returned, contact customer\n" +
                                 "3. If customer unresponsive, initiate re-charge or collection",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send alert during refund compensation for {RefundId}. " +
                "CRITICAL: Manual review required!",
                context.RefundId);
        }
    }
    
    // @todo: make separate class with more dynamic behavior
    private async Task SendCriticalAlertAsync(
        string alertType,
        Guid orderId,
        string refundId,
        string message,
        AlertSeverity severity,
        CancellationToken cancellationToken)
    {
        // in MVP integrate with: pagerDuty, email, sms, slack...fucking something
        
        logger.LogInformation("Sending {Severity} alert: {AlertType} for order {OrderId}",
            severity,
            alertType,
            orderId);

        await Task.CompletedTask;
    }
    
    // @todo make separate class. Request should be sent on help desk
    private async Task CreateManualInterventionTicketAsync(
        Guid orderId,
        string refundId,
        string issue,
        string suggestedAction,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating manual intervention ticket for order {OrderId}",
            orderId);
        
        // create ticket: jira/internal

        await Task.CompletedTask;
    }
}
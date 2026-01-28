using Application.Gateways;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga.Steps;

public sealed class UpdateAccountingRecordsStep(
    IAccountingGateway accountingGateway,
    ILogger<UpdateAccountingRecordsStep> logger) : ISagaStep<ReturnSagaData, ReturnSagaContext>
{
    public string StepName => "UpdateAccountingRecords";
    public int Order => 5;

    public async Task<StepResult> ExecuteAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(context.RefundId))
            {
                return StepResult.Failure("RefundId is required but was not found in context");
            }

            logger.LogInformation(
                "Updating accounting records for order {OrderId}, refund {RefundId}",
                data.CorrelationId,
                context.RefundId);

            //@think is this possible to merge 2 requests into one to not call external system 2 times?
            
            // step 1: record the refund transaction
            var journalEntryId = await accountingGateway.RecordRefundAsync(
                orderId: data.CorrelationId,
                refundId: context.RefundId,
                amount: data.RefundAmount,
                currency: data.Currency,
                reason: data.ReturnReason,
                cancellationToken);

            logger.LogInformation(
                "Refund recorded is accounting. Journal Entry: {JournalEntryId}",
                journalEntryId);

            // step 2: reverse the revenue
            var reversalId = await accountingGateway.ReverseRevenueAsync(
                orderId: data.CorrelationId,
                amount: data.RefundAmount,
                currency: data.Currency,
                returnedItems: data.ReturnedItems,
                cancellationToken);


            context.RevenueReversalId = reversalId;

            logger.LogInformation(
                "Revenue reversed in accounting. Reversal ID: {ReversalId}",
                reversalId);

            return StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["JournalEntryId"] = journalEntryId,
                ["RevenueReversalId"] = reversalId,
                ["Amount"] = data.RefundAmount
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to update accounting records for order {OrderId}",
                data.CorrelationId);

            return StepResult.Failure($"Accounting update failed: {ex.Message}");
        }
    }

    public async Task CompensateAsync(
        ReturnSagaData data,
        ReturnSagaContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.RevenueReversalId))
        {
            logger.LogInformation(
                "No revenue reversal to cancel for order {OrderId}",
                data.CorrelationId);
            return;
        }

        try
        {
            logger.LogInformation(
                "Cancelling revenue reversal {ReversalId} for order {OrderId}",
                context.RevenueReversalId,
                data.CorrelationId);

            await accountingGateway.CancelRevenueReversalAsync(
                reversalId: context.RevenueReversalId,
                reason: "Return saga compensation - return cancelled",
                cancellationToken);
            
            logger.LogInformation(
                "Successfully cancelled revenue reversal {ReversalId}",
                context.RevenueReversalId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to cancel revenue reversal {ReversalId}. " +
                "Manual accounting adjustment may be required.",
                context.RevenueReversalId);
        }
    }
}
using Application.Commands.ReconcilePendingPayments;
using MediatR;
using Microsoft.Extensions.Options;
using Infrastructure.Options;

namespace Infrastructure.BackgroundServices;

internal sealed class PendingPaymentsReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReconciliationWorkerOptions> options,
    ILogger<PendingPaymentsReconciliationWorker> logger) : BackgroundService
{
    private readonly ReconciliationWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.Enabled)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                await RunReconciliationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in pending payments reconciliation worker");
            }

            var interval = NormalizePositive(_options.IntervalSeconds, 60);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ReconcilePendingPaymentsCommand(
            OlderThanMinutes: NormalizePositive(_options.OlderThanMinutes, 15),
            BatchSize: NormalizePositive(_options.BatchSize, 100));

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var errors = result.Errors.Count == 0
                ? "No additional error details were returned."
                : string.Join("; ", result.Errors);

            logger.LogWarning("Pending reconciliation failed. Errors={Errors}", errors);
            return;
        }

        if (result.Value is null)
        {
            logger.LogInformation("Pending reconciliation completed with no aggregate result payload.");
            return;
        }

        logger.LogInformation(
            "Pending reconciliation completed. PaymentsChecked={PaymentsChecked}, PaymentsSucceeded={PaymentsSucceeded}, PaymentsFailed={PaymentsFailed}, RefundsChecked={RefundsChecked}, RefundsSucceeded={RefundsSucceeded}, RefundsFailed={RefundsFailed}, CallbacksQueued={CallbacksQueued}",
            result.Value.PaymentsChecked,
            result.Value.PaymentsSucceeded,
            result.Value.PaymentsFailed,
            result.Value.RefundsChecked,
            result.Value.RefundsSucceeded,
            result.Value.RefundsFailed,
            result.Value.CallbacksQueued);
    }

    private static int NormalizePositive(int value, int fallback)
    {
        return value > 0 ? value : fallback;
    }
}
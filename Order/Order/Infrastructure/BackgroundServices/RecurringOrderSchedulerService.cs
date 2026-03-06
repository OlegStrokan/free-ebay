using Application.Commands.RecurringOrder.ExecuteRecurringOrder;
using Application.Interfaces;
using MediatR;

namespace Infrastructure.BackgroundServices;

public sealed class RecurringOrderSchedulerService(
    IServiceProvider serviceProvider,
    ILogger<RecurringOrderSchedulerService> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _pollIntervalSeconds =
        configuration.GetValue<int>("RecurringOrder:SchedulerIntervalSeconds", 60);

    private readonly int _batchSize =
        configuration.GetValue<int>("RecurringOrder:BatchSize", 50);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        
        logger.LogInformation(
            "RecurringOrderSchedulerService started. PollInterval={PollInterval}s, BatchSize={BatchSize}",
            _pollIntervalSeconds, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueOrdersAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("RecurringOrderSchedulerService stopping...");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in RecurringOrderSchedulerService main loop");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }

        logger.LogInformation("RecurringOrderSchedulerService stopped");
    }

    private async Task ProcessDueOrdersAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IRecurringOrderReadRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var dueOrders = await readRepository.GetDueAsync(DateTime.UtcNow, _batchSize, ct);

        if (!dueOrders.Any())
            return;

        logger.LogInformation("Found {Count} due recurring orders to execute", dueOrders.Count);

        foreach (var summary in dueOrders)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var result = await mediator.Send(
                    new ExecuteRecurringOrderCommand(summary.Id), ct);

                if (result.IsSuccess)
                {
                    if (result.Value != Guid.Empty)
                        logger.LogInformation(
                            "Executed RecurringOrder {RecurringOrderId} → child Order {OrderId}",
                            summary.Id, result.Value);
                    else
                        logger.LogDebug(
                            "RecurringOrder {RecurringOrderId} was not due (concurrent execution skipped)",
                            summary.Id);
                }
                else
                {
                    logger.LogWarning(
                        "Failed to execute RecurringOrder {RecurringOrderId}: {Error}",
                        summary.Id, result.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing RecurringOrder {RecurringOrderId}", summary.Id);
            }
        }
    }
}

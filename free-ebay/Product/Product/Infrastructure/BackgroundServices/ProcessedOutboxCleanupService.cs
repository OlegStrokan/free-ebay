using Application.Interfaces;

namespace Infrastructure.BackgroundServices;

public sealed class ProcessedOutboxCleanupService(
    IServiceProvider serviceProvider,
    ILogger<ProcessedOutboxCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

                using var scope = serviceProvider.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                await outboxRepository.DeleteProcessedMessagesAsync(stoppingToken);

                logger.LogInformation("Cleaned up processed outbox messages older than 7 days");
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, dont delete it because without this after task.delay it throws
                // OperationCancelledException which trigger (Exception ex) - we dont want this
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during processed outbox cleanup");
            }
        }
    }
}

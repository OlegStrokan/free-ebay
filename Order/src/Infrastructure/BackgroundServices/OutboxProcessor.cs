using Application.Interfaces;

namespace Infrastructure.BackgroundServices;

public class OutboxProcessor(
    IServiceProvider serviceProvider,
    IEventPublisher eventPublisher,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var messages = await outboxRepo.GetUnprocessedMessagesAsync(20, stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    await eventPublisher.PublishRawAsync(message.Id, message.Type, message.Content, message.OccurredOnUtc, stoppingToken);
                    await outboxRepo.MarkAsProcessedAsync(message.Id, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
                    // it don't mark as processed so  it will be retried in the next loop
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
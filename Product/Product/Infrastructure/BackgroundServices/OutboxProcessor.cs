using Application.Interfaces;

namespace Infrastructure.BackgroundServices;

public sealed class OutboxProcessor(
    IServiceProvider serviceProvider,
    IEventPublisher eventPublisher,
    ILogger<OutboxProcessor> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _batchSize = configuration.GetValue<int>("Outbox:BatchSize", 20);
    private readonly int _maxRetries = configuration.GetValue<int>("Outbox:MaxRetries", 5);
    private readonly int _pollIntervalMs = configuration.GetValue<int>("Outbox:PollIntervalMs", 2000);
    private readonly int _maxParallelism = configuration.GetValue<int>("Outbox:MaxParallelism", 5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield so the host startup isn't blocked type shit
        await Task.Yield();

        logger.LogInformation(
            "OutboxProcessor started. BatchSize={BatchSize}, MaxParallelism={Parallelism}",
            _batchSize, _maxParallelism);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMilliseconds(_pollIntervalMs), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("OutboxProcessor stopping...");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in OutboxProcessor main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("OutboxProcessor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var exhaustedCount = await outboxRepository.MarkRetryExhaustedMessagesAsProcessedAsync(
            _batchSize,
            _maxRetries,
            ct);

        if (exhaustedCount > 0)
        {
            logger.LogError(
                "Marked {Count} retry-exhausted outbox messages as processed to stop retry loops",
                exhaustedCount);
        }

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(_batchSize, _maxRetries, ct);
        if (messages.Count == 0)
            return;

        logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        // Group by AggregateId to preserve causal ordering per aggregate;
        // different aggregates run in parallel.
        var groups = messages
            .GroupBy(m => m.AggregateId)
            .Select(g => g.OrderBy(m => m.OccurredOn).ToList())
            .ToList();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(groups, options, async (group, groupCt) =>
        {
            using var groupScope = serviceProvider.CreateScope();
            var groupOutbox = groupScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            foreach (var message in group)
            {
                try
                {
                    await eventPublisher.PublishRawAsync(
                        message.Id,
                        message.Type,
                        message.Content,
                        message.OccurredOn,
                        message.AggregateId,
                        groupCt);

                    await groupOutbox.MarkAsProcessedAsync(message.Id, groupCt);

                    logger.LogDebug("Published outbox message {MessageId} ({Type})", message.Id, message.Type);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to publish outbox message {MessageId} (attempt {Attempt}/{MaxRetries})",
                        message.Id, message.RetryCount + 1, _maxRetries);

                    await groupOutbox.IncrementRetryCountAsync(message.Id, ex.Message, groupCt);
                }
            }
        });
    }
}

using Application.Interfaces;
using Application.Models;

namespace Infrastructure.BackgroundServices;

public sealed class OutboxProcessor(
    IServiceProvider serviceProvider,
    IEventPublisher eventPublisher,
    ILogger<OutboxProcessor> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _batchSize = configuration.GetValue<int>("Outbox:BatchSize", 20);
    private readonly int _maxRetries = configuration.GetValue<int>("Outbox:MaxRetries", 5);
    private readonly int _maxAgeDays = configuration.GetValue<int>("Outbox:MaxAgeDays", 7);
    private readonly int _pollIntervalMs = configuration.GetValue<int>("Outbox:PollInternalMs", 2000);
    private readonly int _maxDegreeOfParallelism = configuration.GetValue<int>("Outbox:MaxParallelism", 5);


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OutboxProcessor started with BatchSize={BatchSize}, MaxParallelism={Parallelism}",
            _batchSize,
            _maxDegreeOfParallelism);

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
            
            logger.LogInformation("OutboxProcessor stopped");
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var deadLetterRepository = scope.ServiceProvider.GetRequiredService<IDeadLetterRepository>();

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(_batchSize, stoppingToken);

        if (!messages.Any())
            return;
        
        logger.LogDebug("Processing {Count} outbox messages", messages.Count());

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            CancellationToken = stoppingToken
        };

        await Parallel.ForEachAsync(messages, options, async (message, ct) =>
        {
            await ProcessMessageAsync(message, outboxRepository, deadLetterRepository, ct);
        });
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IOutboxRepository outboxRepository,
        IDeadLetterRepository deadLetterRepository,
        CancellationToken ct)
    {
        try
        {
            var messageAge = DateTime.UtcNow - message.OccurredOnUtc;
            if (messageAge.TotalDays > _maxAgeDays)
            {
                logger.LogWarning(
                    "Message {MessageId} is {Days} days old (max: {MaxDays}). Moving to dead letter queue.",
                    message.Id,
                    messageAge.TotalDays,
                    _maxAgeDays);

                await MoveToDeadLetterAsync(
                    message,
                    $"Message exceeded maximum age of {_maxAgeDays} days",
                    deadLetterRepository,
                    outboxRepository,
                    ct);
                return;
            }

            if (message.RetryCount >= _maxRetries)
            {
                logger.LogWarning(
                    "Message {MessageId} exceeded max reteids ({MaxRetries}). Moving to dead letter queue.",
                    message.Id,
                    _maxRetries);

                await MoveToDeadLetterAsync(
                    message,
                    $"Message exceeded maximum retry count of {_maxRetries}",
                    deadLetterRepository,
                    outboxRepository,
                    ct);
                return;
            }

            await eventPublisher.PublishRawAsync(
                message.Id,
                message.Type,
                message.Content,
                message.OccurredOnUtc,
                ct);

            await outboxRepository.MarkAsProcessedAsync(message.Id, ct);

            logger.LogDebug(
                "Successfully published message {MessageId} of type {Type}",
                message.Id,
                message.Type);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish message {MessageId} (attempt {Attempt}/{MaxRetries})",
                message.Id,
                message.RetryCount + 1,
                _maxRetries);

            await outboxRepository.IncrementRetryCountAsync(message.Id, ct);
        }
    }

    private async Task MoveToDeadLetterAsync(
        OutboxMessage message,
        string reason,
        IDeadLetterRepository deadLetterRepository,
        IOutboxRepository outboxRepo,
        CancellationToken ct)
    {
        try
        {
            await deadLetterRepository.AddAsync(
                message.Id,
                message.Type,
                message.Content,
                message.OccurredOnUtc,
                reason,
                message.RetryCount,
                ct);

            await outboxRepo.DeleteAsync(message.Id, ct);

            logger.LogWarning(
                "Moved message {MessageId} to dead letter queue. Reason: {Reason}",
                message.Id,
                reason);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to move message {MessageId} to dead letter queue",
                message.Id);
        }
    }
    
}
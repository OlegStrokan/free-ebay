using Application.Interfaces;
using Application.Models;
using Infrastructure.Services;

namespace Infrastructure.BackgroundServices;

public sealed class KafkaReadModelRetryWorker(
    IServiceProvider serviceProvider,
    ILogger<KafkaReadModelRetryWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _batchSize =
        configuration.GetValue<int>("KafkaRetry:BatchSize", 20);
    private readonly int _workerRetryLimit =
        configuration.GetValue<int>("KafkaRetry:WorkerRetryLimit", 5);
    private readonly int _pollIntervalSeconds =
        configuration.GetValue<int>("KafkaRetry:WorkerPollIntervalSeconds", 30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        logger.LogInformation(
            "KafkaReadModelRetryWorker started. PollInterval={Interval}s, RetryLimit={Limit}, BatchSize={Batch}",
            _pollIntervalSeconds, _workerRetryLimit, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRecordsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in KafkaReadModelRetryWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("KafkaReadModelRetryWorker stopped");
    }

    private async Task ProcessDueRecordsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var retryRepo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        var dueRecords = await retryRepo.GetDueRecordsAsync(_batchSize, ct);
        if (dueRecords.Count == 0) return;

        logger.LogInformation("KafkaReadModelRetryWorker processing {Count} due records", dueRecords.Count);

        foreach (var record in dueRecords)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessSingleRecordAsync(record, ct);
        }
    }

    private async Task ProcessSingleRecordAsync(KafkaRetryRecord record, CancellationToken ct)
    {
        // Each record gets its own scope so its DB context lifetime is independent
        using var scope = serviceProvider.CreateScope();
        var retryRepo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        try
        {
            await retryRepo.MarkInProgressAsync(record.Id, ct);

            var dispatcher = scope.ServiceProvider.GetRequiredService<ReadModelEventDispatcher>();
            var aggregateId = record.MessageKey ?? string.Empty;

            await dispatcher.DispatchAsync(record.EventType, aggregateId, record.Payload, ct);

            await retryRepo.MarkSucceededAsync(record.Id, ct);

            logger.LogInformation(
                "KafkaRetryRecord {RecordId} succeeded (event={EventType})",
                record.Id, record.EventType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var newRetryCount = record.RetryCount + 1;

            if (newRetryCount >= _workerRetryLimit)
            {
                logger.LogError(
                    ex,
                    "KafkaRetryRecord {RecordId} exceeded retry limit ({Limit}) — moving to DeadLetter",
                    record.Id, _workerRetryLimit);

                await retryRepo.MarkDeadLetterAsync(
                    record.Id,
                    Truncate(ex.Message, 2000),
                    ex.GetType().FullName,
                    ct);

                try
                {
                    var dlq = scope.ServiceProvider.GetRequiredService<IDeadLetterRepository>();
                    await dlq.AddAsync(
                        messageId: Guid.NewGuid(),
                        type: "KafkaReadModelRetryExhausted",
                        content: record.Payload,
                        occuredOn: record.FirstFailureTime,
                        failureReason: Truncate(ex.Message, 2000) ?? string.Empty,
                        retryCount: newRetryCount,
                        aggregateId: record.MessageKey ?? string.Empty,
                        ct: ct);
                }
                catch (Exception dlqEx)
                {
                    logger.LogError(
                        dlqEx,
                        "Failed to write KafkaRetryRecord {RecordId} to DeadLetterMessages table",
                        record.Id);
                }
            }
            else
            {
                var jitterMinutes = Random.Shared.Next(0, 10);
                var nextRetryAt = DateTime.UtcNow.AddMinutes(3 + jitterMinutes);

                logger.LogWarning(
                    ex,
                    "KafkaRetryRecord {RecordId} failed (attempt {Attempt}/{Limit}), rescheduling for {NextRetry}",
                    record.Id, newRetryCount, _workerRetryLimit, nextRetryAt);

                await retryRepo.RescheduleAsync(
                    record.Id,
                    newRetryCount,
                    nextRetryAt,
                    Truncate(ex.Message, 2000),
                    ex.GetType().FullName,
                    ct);
            }
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null
        : value.Length <= maxLength ? value
        : value[..maxLength];
}

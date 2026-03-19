using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Callbacks;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundServices;

internal sealed class OrderCallbackDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OrderCallbackOptions> callbackOptions,
    ILogger<OrderCallbackDeliveryWorker> logger) : BackgroundService
{
    private readonly OrderCallbackOptions _callbackOptions = callbackOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed > 0)
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in outbound callback delivery worker");
            }

            var pause = NormalizePositive(_callbackOptions.PollIntervalSeconds, 5);
            await Task.Delay(TimeSpan.FromSeconds(pause), stoppingToken);
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var callbackRepository = scope.ServiceProvider.GetRequiredService<IOutboundOrderCallbackRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOrderCallbackDispatcher>();

        var now = clock.UtcNow;
        var batchSize = NormalizePositive(_callbackOptions.BatchSize, 100);

        var callbacks = await callbackRepository.GetPendingAsync(now, batchSize, cancellationToken);
        if (callbacks.Count == 0)
        {
            return 0;
        }

        var maxAttempts = NormalizePositive(_callbackOptions.MaxAttempts, 8);
        var changed = 0;

        foreach (var callback in callbacks)
        {
            if (!callback.CanAttempt(now))
            {
                continue;
            }

            var attemptedAt = clock.UtcNow;
            var delivery = await dispatcher.DispatchAsync(callback, cancellationToken);

            if (delivery.Succeeded)
            {
                callback.MarkDelivered(attemptedAt);
                await callbackRepository.UpdateAsync(callback, cancellationToken);
                changed++;
                continue;
            }

            var errorMessage = string.IsNullOrWhiteSpace(delivery.Error)
                ? "Order callback delivery failed."
                : delivery.Error;

            var currentAttempt = callback.AttemptCount + 1;

            if (currentAttempt >= maxAttempts)
            {
                callback.MarkPermanentFailure(errorMessage, attemptedAt);
            }
            else
            {
                var delay = CalculateRetryDelay(currentAttempt);
                callback.MarkAttemptFailed(errorMessage, attemptedAt.Add(delay), attemptedAt);
            }

            await callbackRepository.UpdateAsync(callback, cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        logger.LogDebug(
            "Outbound callback delivery cycle completed. Loaded={LoadedCount}, Updated={UpdatedCount}",
            callbacks.Count,
            changed);

        return changed;
    }

    private TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        var baseSeconds = NormalizePositive(_callbackOptions.BaseRetryDelaySeconds, 5);
        var maxSeconds = Math.Max(baseSeconds, NormalizePositive(_callbackOptions.MaxRetryDelaySeconds, 300));

        var safeAttempt = Math.Max(1, attemptNumber);
        var growth = Math.Pow(2, safeAttempt - 1);
        var delay = Math.Min(maxSeconds, baseSeconds * growth);

        return TimeSpan.FromSeconds(delay);
    }

    private static int NormalizePositive(int value, int fallback)
    {
        return value > 0 ? value : fallback;
    }
}
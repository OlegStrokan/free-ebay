using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundServices;

public sealed class ReservationExpiryProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationExpiryOptions> options,
    ILogger<ReservationExpiryProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while expiring stale inventory reservations.");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(options.Value.PollIntervalMs),
                stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();

        var cutoffUtc = DateTime.UtcNow - options.Value.ReservationTtl;
        var expiredCount = await store.ExpireStaleReservationsAsync(
            cutoffUtc,
            options.Value.BatchSize,
            cancellationToken);

        if (expiredCount > 0)
        {
            logger.LogInformation(
                "Expired {ExpiredCount} stale inventory reservations older than {CutoffUtc}.",
                expiredCount,
                cutoffUtc);
        }
    }
}
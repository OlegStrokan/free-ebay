using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BackgroundServices;

// background service that cleans up old processedEvents record
// run daily and remove events older than configured retention period
public class ProcessedEventsCleanupService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ProcessedEventsCleanupService> logger;
    private readonly TimeSpan retentionPeriod;
    private readonly TimeSpan cleanupInterval;


    public ProcessedEventsCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ProcessedEventsCleanupService> logger,
        IConfiguration configuration)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;

        var retentionDays = configuration.GetValue<int>("ProcessedEvents:RetentionDays", 30);
        retentionPeriod = TimeSpan.FromDays(retentionDays);

        var cleanupHours = configuration.GetValue<int>("ProcessedEvents:CleanupIntervalHours", 24);
        cleanupInterval = TimeSpan.FromHours(cleanupHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Processed events cleanup service started. " +
            "Retention: {Retention} days, Clenup interval: {Interval} hours",
            retentionPeriod.TotalDays,
            cleanupInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during processed events cleanup");
            }

            await Task.Delay(cleanupInterval, stoppingToken);
        }
        
        logger.LogInformation("Processed events cleanup service stopped");
    }

    private async Task CleanupOldEventAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        
        logger.LogInformation(
            "Cleaned up processed events older than {CutoffDate}",
            cutoffDate);

        int totalDeleted = 0;
        int batchSize = 1000;

        while (!cancellationToken.IsCancellationRequested)
        {
            var deleted = await dbContext.ProcessedEvents
                .Where(e => e.ProcessedAt < cutoffDate)
                .Take(batchSize)
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deleted;

            if (deleted < batchSize)
                break;

            await Task.Delay(100, cancellationToken);
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation(
                "Cleaned up {Count} processed events older than {Days} days,",
                totalDeleted,
                retentionPeriod.TotalDays);
        }
        else
        {
            logger.LogDebug("No old processed events to clean up");
        }
    }
}
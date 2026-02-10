using Application.Sagas;
using Application.Sagas.OrderSaga;
using Application.Sagas.Persistence;
using Application.Sagas.ReturnSaga;

namespace Infrastructure.BackgroundServices;

public class SagaWatchdogService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaWatchdogService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _stuckThreshold = TimeSpan.FromMinutes(5); // "stuck" if no update for 5 mins

    public SagaWatchdogService(
        IServiceProvider serviceProvider,
        ILogger<SagaWatchdogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Saga Watchdog started. Poll interval: {PollInterval}, Stuck threshold: {StuckThreshold}",
            _checkInterval, _stuckThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRecoverStuckSagaAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Saga Watchdog execution");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Saga Watchdog stopped");
    }

    private async Task CheckAndRecoverStuckSagaAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        _logger.LogDebug("Checking for stuck sagas...");

        var cutoffTime = DateTime.UtcNow - _stuckThreshold;
        var stuckSagas = await sagaRepository.GetStuckSagasAsync(cutoffTime, cancellationToken);

        if (stuckSagas.Count == 0)
        {
            _logger.LogDebug("No stuck sagas found");
            return;
        }

        _logger.LogWarning(
            "Found {Count} stuck sagas haven't updated since {Cutoff}", stuckSagas.Count, cutoffTime);

        foreach (var saga in stuckSagas)
        {
            await HandleStuckSagaAsync(saga, cancellationToken);
        }
    }


    private async Task HandleStuckSagaAsync(SagaState saga, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        _logger.LogWarning(
            "Processing stuck saga {SagaId} ({SagaType}). " +
            "Correlation: {CorrelationId} Status: {Status}, Current Step: {CurrentStep}, " +
            "Last Updated: {UpdatedAt}",
            saga.Id, saga.SagaType, saga.CorrelationId, saga.Status,
            saga.CurrentStep ?? "None", saga.UpdatedAt);

        try
        {
            if (await IsSagaActuallyCompletedAsync(saga, sagaRepository, cancellationToken))
            {
                _logger.LogInformation(
                    "Saga {SagaId} has all steps completed. Marking as Completed.", saga.Id);

                saga.Status = SagaStatus.Completed;
                saga.UpdatedAt = DateTime.UtcNow;
                await sagaRepository.SaveAsync(saga, cancellationToken);
                return;
            }

            var timeSinceUpdate = DateTime.UtcNow - saga.UpdatedAt;

            if (timeSinceUpdate > _stuckThreshold * 2)
            {
                _logger.LogError(
                    "Saga {SagaId} stuck for {Duration}. Marking as failed and compensating.",
                    saga.Id, timeSinceUpdate);

                await FailAndCompensateSagaAsync(saga, scope, cancellationToken);
                return;
            }

            _logger.LogWarning(
                "Saga {SagaId} stuck {Duration} but within tolerance. " +
                "Will compensate if still stuck on next check.",
                saga.Id, timeSinceUpdate);
        }

        catch (Exception ex)
        {
            _logger.LogError("Failed to handle stuck saga {SagaId}", saga.Id);
        }
    }

    private async Task<bool> IsSagaActuallyCompletedAsync(
        SagaState saga,
        ISagaRepository sagaRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var steps = await sagaRepository.GetStepLogsAsync(saga.Id, cancellationToken);

            if (!steps.Any())
            {
                _logger.LogDebug("Saga {SagaId} has no steps recorded", saga.Id);
                return false;
            }

            var allCompensated = steps.All(s => s.Status == StepStatus.Completed);

            if (allCompensated)
            {
                _logger.LogInformation(
                    "Saga {SagaId} has all {Count} steps completed", saga.Id, steps.Count);
            }

            return allCompensated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Count not verify completion for saga {SagaId}", saga.Id);
            return false;
        }
    }

    private async Task FailAndCompensateSagaAsync(
        SagaState saga,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        saga.Status = SagaStatus.Failed;
        saga.UpdatedAt = DateTime.UtcNow;
        await sagaRepository.SaveAsync(saga, cancellationToken);

        try
        {
            var compensationResult = await TryCompensateSagaAsync(saga, scope, cancellationToken);

            if (compensationResult)
            {
                _logger.LogInformation("Successfully compensated saga {SagaId}", saga.Id);
            }
            else
            {
                _logger.LogError(
                    "No saga handler found for {SagaType}. " +
                    "Manual compensation required for saga {SagaId}",
                    saga.SagaType, saga.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "CRITICAL: Failed to compensate saga {SagaId}. Manual intervention required!",
                saga.Id);

            saga.Status = SagaStatus.FailedToCompensate;
            await sagaRepository.SaveAsync(saga, cancellationToken);
        }
    }

    private async Task<bool> TryCompensateSagaAsync(
        SagaState saga,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to compensate {SagaType} saga {SagaId}",
            saga.SagaType, saga.Id);

        try
        {
            switch (saga.SagaType)
            {
                case "OrderSaga":
                {
                    var orderSaga = scope.ServiceProvider
                        .GetService<IOrderSaga>();

                    if (orderSaga != null)
                    {
                        await orderSaga.CompensateAsync(saga.Id, cancellationToken);
                        return true;
                    }

                    break;
                }

                case "ReturnSaga":
                {
                    var returnSaga = scope.ServiceProvider
                        .GetService<IReturnSaga>();

                    if (returnSaga != null)
                    {
                        await returnSaga.CompensateAsync(saga.Id, cancellationToken);
                        return true;
                    }

                    break;
                }
            }

            _logger.LogWarning("No handler registered for saga type {SagaType}", saga.SagaType);

            return false;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating saga {SagaId}", saga.Id);
            throw;
        }
    }

}
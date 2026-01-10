using System.Text.Json;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas;

public abstract class SagaBase<TData, TContext> : ISaga<TData>
    where TData : SagaData
    where TContext : SagaContext, new()
{
    private readonly ISagaRepository _repository;
    private readonly ILogger _logger;
    protected readonly IEnumerable<ISagaStep<TData, TContext>> Steps;
    protected abstract string SagaType { get;  }
    
    protected SagaBase(
        ISagaRepository repository,
        IEnumerable<ISagaStep<TData, TContext>> steps,
        ILogger logger
        )
    {
        _repository = repository;
        _logger = logger;
        // ensure that steps register in correct order
        Steps = steps.OrderBy(s => s.Order).ToList();
    }
    
    public async Task<SagaResult> ExecuteAsync(TData data, CancellationToken cancellationToken)
    {
        var sagaId = Guid.NewGuid();
        var context = new TContext();

        var sagaState = new SagaState
        {
            Id = sagaId,
            CorrelationId = data.CorrelationId,
            Status = SagaStatus.Running,
            SagaType = SagaType,
            Payload = JsonSerializer.Serialize(data),
            Context = JsonSerializer.Serialize(context),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repository.SaveAsync(sagaState, cancellationToken);
        
        _logger.LogInformation("Started {SagaType} saga {SagaId} for correlation {CorrelationId}",
            SagaType, sagaId, data.CorrelationId);

        foreach (var step in Steps)
        {
            var stepResult = await ExecuteStepAsync(
                sagaId,
                step,
                data,
                context,
                cancellationToken);

            if (!stepResult.Success)
            {
                _logger.LogWarning(
                    "Step {StepName} failed in saga {SagaId}. Starting compensation...",
                    step.StepName, sagaId);

                await CompensateAsync(sagaId, cancellationToken);
                return SagaResult.Failed(sagaId, stepResult.ErrorMessage ?? "Unknown error");
            }

            sagaState.Context = JsonSerializer.Serialize(context);
            sagaState.CurrentStep = step.StepName;
            sagaState.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveAsync(sagaState, cancellationToken);
        }

        sagaState.Status = SagaStatus.Completed;
        sagaState.UpdatedAt = DateTime.Now;
        await _repository.SaveAsync(sagaState, cancellationToken);

        _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);
        return SagaResult.Success(sagaId);
    }

    private async Task<StepResult> ExecuteStepAsync(
        Guid sagaId,
        ISagaStep<TData, TContext> step,
        TData data,
        TContext context,
        CancellationToken cancellationToken)
    {
        var stepLog = new SagaStepLog
        {
            Id = Guid.NewGuid(),
            SagaId = sagaId,
            StepName = step.StepName,
            Status = StepStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation(
                "Executing step {StepName} in saga {SagaId}",
                step.StepName, sagaId);

            var startTime = DateTime.UtcNow;
            var result = await step.ExecuteAsync(data, context, cancellationToken);
            var endTime = DateTime.UtcNow;

            stepLog.CompletedAt = endTime;
            stepLog.DurationMs = (int)(endTime - startTime).TotalMicroseconds;
            stepLog.Status = result.Success ? StepStatus.Completed : StepStatus.Failed;
            stepLog.ErrorMessage = result.ErrorMessage;

            await _repository.SaveStepAsync(stepLog, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Exception in step {StepName} of saga {SagaId}", step.StepName, sagaId
                );

            stepLog.Status = StepStatus.Failed;
            stepLog.ErrorMessage = ex.Message;
            stepLog.CompletedAt = DateTime.UtcNow;

            await _repository.SaveStepAsync(stepLog, cancellationToken);

            return StepResult.Failure(ex.Message);
        }
    }

    public async Task<SagaResult> CompensateAsync(Guid sagaId, CancellationToken cancellationToken)
    {
        var sagaState = await _repository.GetByIdAsync(sagaId, cancellationToken);
        if (sagaState == null)
            throw new InvalidOperationException($"Saga {sagaId} not found");

        if (sagaState.Status == SagaStatus.Compensated)
        {
            _logger.LogInformation("Saga {SagaId} already compensated", sagaId);
            return SagaResult.Compensated(sagaId);
        }

        sagaState.Status = SagaStatus.Compensating;
        await _repository.SaveAsync(sagaState, cancellationToken);
        
        _logger.LogInformation("Starting compensation for saga {SagaId}", sagaId);

        var data = JsonSerializer.Deserialize<TData>(sagaState.Payload)!;
        var context = JsonSerializer.Deserialize<TContext>(sagaState.Context)!;
        
        var completedSteps = sagaState.Steps
            .Where(s => s.Status == StepStatus.Completed)
            .Select(s => s.StepName)
            .ToHashSet();
        
        // compensate in reverse order
        foreach (var step in Steps.Reverse())
        {
            if (!completedSteps.Contains(step.StepName))
                continue;

            try
            {
                _logger.LogInformation(
                    "Compensating step {StepName} in saga {SagaId}",
                    step.StepName, sagaId);

                await step.CompensateAsync(data, context, cancellationToken);

                // mark step as compensated
                var stepLog = sagaState.Steps.First(s => s.StepName == step.StepName);
                stepLog.Status = StepStatus.Compensated;
                await _repository.SaveStepAsync(stepLog, cancellationToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to compensate step {StepName} in saga {SagaId}. Continuing...",
                    step.StepName, sagaId);
                // dont throw, continue compensating other steps
            }
        }

        sagaState.Status = SagaStatus.Compensated;
        sagaState.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveAsync(sagaState, cancellationToken);
            
         _logger.LogInformation("Saga {SagaId} compensated", sagaId);
        return SagaResult.Compensated(sagaId);
    }
}












































using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Sagas;

public abstract class SagaBase<TData, TContext> : ISagaBase<TData>
    where TData : SagaData
    where TContext : SagaContext, new()
{
    private readonly ISagaRepository _sagaRepository;
    private readonly ILogger _logger;
    private readonly ISagaErrorClassifier _errorClassifier;
    protected readonly IEnumerable<ISagaStep<TData, TContext>> Steps;
    protected abstract string SagaType { get; }

    protected SagaBase(
        ISagaRepository sagaRepository,
        IEnumerable<ISagaStep<TData, TContext>> steps,
        ISagaErrorClassifier errorClassifier,
        ILogger logger)
    {
        _sagaRepository = sagaRepository;
        _logger = logger;
        _errorClassifier = errorClassifier;
        Steps = steps.OrderBy(s => s.Order).ToList();
    }

    public async Task<SagaResult> ExecuteAsync(TData data, CancellationToken cancellationToken)
    {
        /* @think: what if .... you know... we could create timeout cancellation token
        timespan = 5 min
        use createLinkedTokenSource
        pass to all call instead of cancellationToken
        
        */
        
        /* @think: what if we will add like 400 lines of divined code to create event
         driven stuff + adding 30-40 lines to sagaBase class?
         now we have logs, but if Berezovsky will lose money because of us?
         
         
         UPD: it's too complex so we use SagaWatchdogService for pooling data
      */
        
        
        var sagaId = Guid.NewGuid();
        var context = new TContext(); 

        _logger.LogInformation(
            "Starting {SagaType} for correlation {CorrelationId} with {StepCount} steps",
            SagaType, data.CorrelationId, Steps.Count());

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

        await _sagaRepository.SaveAsync(sagaState, cancellationToken);

        _logger.LogInformation(
            "Started {SagaType} saga {SagaId} for correlation {CorrelationId}",
            SagaType, sagaId, data.CorrelationId);

        foreach (var step in Steps)
        {
            _logger.LogInformation(
                "Executing step {StepName} (order: {Order}) for {CorrelationId}",
                step.StepName, step.Order, data.CorrelationId);

            var stepResult = await ExecuteStepAsync(
                sagaId,
                step,
                data,
                context, 
                cancellationToken);

            sagaState.Context = JsonSerializer.Serialize(context);
            sagaState.CurrentStep = step.StepName;
            sagaState.UpdatedAt = DateTime.UtcNow;

            if (stepResult.Metadata.TryGetValue("SagaState", out var sagaStateValue) &&
                sagaStateValue.ToString() == "WaitingForEvent")
            {
                _logger.LogInformation(
                    "Step {StepName} marked saga as waiting for external event.",
                    step.StepName);

                sagaState.Status = SagaStatus.WaitingForEvent;
                await _sagaRepository.SaveAsync(sagaState, cancellationToken);

                return SagaResult.Success(sagaId);
            }

            if (!stepResult.Success)
            {
                _logger.LogWarning(
                    "Step {StepName} failed in saga {SagaId}. Starting compensation...",
                    step.StepName, sagaId);

                sagaState.Status = SagaStatus.Failed;
                await _sagaRepository.SaveAsync(sagaState, cancellationToken);
                
                await CompensateAsync(sagaId, cancellationToken);
                return SagaResult.Failed(sagaId, stepResult.ErrorMessage ?? "Unknown error");
            }
            
            await _sagaRepository.SaveAsync(sagaState, cancellationToken);
        }

        sagaState.Status = SagaStatus.Completed;
        sagaState.UpdatedAt = DateTime.UtcNow;
        await _sagaRepository.SaveAsync(sagaState, cancellationToken);

        _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);
        return SagaResult.Success(sagaId);
    }

    public async Task<SagaResult> ResumeFromStepAsync(
        TData data,
        SagaContext context,
        string fromStepName,
        CancellationToken cancellationToken)
    {

        _logger.LogInformation(
            "Resuming {SagaType} for correlation {CorrelationId} from step {StepName}",
            SagaType, data.CorrelationId, fromStepName);

        var typedContext = (TContext)context; // Safe because of generic constraints
        
        var sagaState = await _sagaRepository.GetByCorrelationIdAsync(
            data.CorrelationId,
            SagaType,
            cancellationToken);

        if (sagaState == null)
        {
            _logger.LogError(
                "Cannot resume {SagaType} for {CorrelationId} - saga state not found",
                SagaType, data.CorrelationId);
            return SagaResult.Failed(Guid.Empty, "Saga state not found");
        }

        if (sagaState.Status != SagaStatus.WaitingForEvent)
        {
            _logger.LogWarning(
                "Saga {SagaId} is in status {Status}, expected WaitingForEvent.",
                sagaState.Id, sagaState.Status);

            if (sagaState.Status == SagaStatus.Completed)
            {
                return SagaResult.Success(sagaState.Id);
            }

            return SagaResult.Failed(sagaState.Id, $"Saga not in waiting state: {sagaState.Status}");
        }

        sagaState.Status = SagaStatus.Running;
        sagaState.UpdatedAt = DateTime.UtcNow;
        sagaState.Context = JsonSerializer.Serialize(typedContext);        await _sagaRepository.SaveAsync(sagaState, cancellationToken);

        var resumeStep = Steps.FirstOrDefault(s => s.StepName == fromStepName);
        if (resumeStep == null)
        {
            return SagaResult.Failed(sagaState.Id, $"Step {fromStepName} not found");
        }

        var remainingSteps = Steps
            .Where(s => s.Order >= resumeStep.Order)
            .OrderBy(s => s.Order)
            .ToList();

        foreach (var step in remainingSteps)
        {
            var stepResult = await ExecuteStepAsync(
                sagaState.Id, step, data, typedContext, cancellationToken);

            sagaState.Context = JsonSerializer.Serialize(typedContext); 
            sagaState.CurrentStep = step.StepName;
            sagaState.UpdatedAt = DateTime.UtcNow;

            if (stepResult.Metadata.TryGetValue("SagaState", out var sagaStateValue) &&
                sagaStateValue?.ToString() == "WaitingForEvent")
            {
                sagaState.Status = SagaStatus.WaitingForEvent;
                await _sagaRepository.SaveAsync(sagaState, cancellationToken);
                return SagaResult.Success(sagaState.Id);
            }

            if (!stepResult.Success)
            {
                await CompensateAsync(sagaState.Id, cancellationToken);
                return SagaResult.Failed(sagaState.Id, stepResult.ErrorMessage ?? "Unknown error");
            }

            await _sagaRepository.SaveAsync(sagaState, cancellationToken);
        }

        sagaState.Status = SagaStatus.Completed;
        sagaState.UpdatedAt = DateTime.UtcNow;
        await _sagaRepository.SaveAsync(sagaState, cancellationToken);

        return SagaResult.Success(sagaState.Id);
    }

    private async Task<StepResult> ExecuteStepAsync(
        Guid sagaId,
        ISagaStep<TData, TContext> step,
        TData data,
        TContext context,
        CancellationToken cancellationToken)
    {
        // @think: right now we have boilerplate retries
        // maybe we can use retry decorator which used by external systems (gateway calls)

        const int maxRetries = 3;
        int retryCount = 0;

        while (true)
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
                var startTime = DateTime.UtcNow;
                var result = await step.ExecuteAsync(data, context, cancellationToken);
                var endTime = DateTime.UtcNow;

                stepLog.CompletedAt = endTime;
                stepLog.DurationMs = (int)(endTime - startTime).TotalMilliseconds;
                stepLog.Status = result.Success ? StepStatus.Completed : StepStatus.Failed;
                stepLog.ErrorMessage = result.ErrorMessage;

                await _sagaRepository.SaveStepAsync(stepLog, cancellationToken);

                return result;
            }

            catch (Exception ex) when (_errorClassifier.IsTransient(ex) && retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "Transient error in {StepName}. Retry {Count}/{Max}",
                    step.StepName, retryCount, maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in step {StepName} of saga {SagaId}", step.StepName, sagaId);

                stepLog.Status = StepStatus.Failed;
                stepLog.ErrorMessage = ex.Message;
                stepLog.CompletedAt = DateTime.UtcNow;

                await _sagaRepository.SaveStepAsync(stepLog, cancellationToken);

                return StepResult.Failure(ex.Message);
            }
        }
    }

    public async Task<SagaResult> CompensateAsync(Guid sagaId, CancellationToken cancellationToken)
    {
        var sagaState = await _sagaRepository.GetByIdAsync(sagaId, cancellationToken);
        if (sagaState == null)
            throw new InvalidOperationException($"Saga {sagaId} not found");

        if (sagaState.Status == SagaStatus.Compensated)
        {
            return SagaResult.Compensated(sagaId);
        }

        sagaState.Status = SagaStatus.Compensating;
        await _sagaRepository.SaveAsync(sagaState, cancellationToken);

        var data = JsonSerializer.Deserialize<TData>(sagaState.Payload)!;
        var context = JsonSerializer.Deserialize<TContext>(sagaState.Context) ?? new TContext();
        
        var completedSteps = sagaState.Steps
            .Where(s => s.Status == StepStatus.Completed)
            .Select(s => s.StepName)
            .ToHashSet();

        foreach (var step in Steps.Reverse())
        {
            if (!completedSteps.Contains(step.StepName))
                continue;

            int retryCount = 0;
            const int maxRetries = 3;

            while (true)
            {
                try
                {
                    await step.CompensateAsync(data, context, cancellationToken);

                    var stepLog = sagaState.Steps.First(s => s.StepName == step.StepName);
                    stepLog.Status = StepStatus.Compensated;
                    await _sagaRepository.SaveCompensationStateAsync(
                        sagaState, stepLog, cancellationToken);

                    break;
                }
                catch (Exception ex) when (_errorClassifier.IsTransient(ex) && retryCount < maxRetries)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Compensation transient failure for {StepName}. Retry {Count}/{Max}",
                        step.StepName, retryCount, maxRetries);

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "FATAL: Compensation failed permanently for {StepName} in saga {SagaId}",
                        step.StepName, sagaId);

                    sagaState.Status = SagaStatus.FailedToCompensate;
                    await _sagaRepository.SaveAsync(sagaState, cancellationToken);
                    throw;
                }
            }
        }

        sagaState.Status = SagaStatus.Compensated;
        sagaState.UpdatedAt = DateTime.UtcNow;
        await _sagaRepository.SaveAsync(sagaState, cancellationToken);

        return SagaResult.Compensated(sagaId);
    }
}












































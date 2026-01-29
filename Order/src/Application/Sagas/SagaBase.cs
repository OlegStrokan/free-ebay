using System.Text.Json;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas;

public abstract class SagaBase<TData, TContext> : ISaga<TData>
    where TData : SagaData
    where TContext : SagaContext, new()
{
    private readonly ISagaRepository _sagaRepository;
    private readonly ILogger _logger;
    protected readonly IEnumerable<ISagaStep<TData, TContext>> Steps;
    protected abstract string SagaType { get;  }
    
    protected SagaBase(
        ISagaRepository sagaRepository,
        IEnumerable<ISagaStep<TData, TContext>> steps,
        ILogger logger
        )
    {
        _sagaRepository = sagaRepository;
        _logger = logger;
        // ensure that steps register in correct order
        Steps = steps.OrderBy(s => s.Order).ToList();
    }
    
    public async Task<SagaResult> ExecuteAsync(TData data, CancellationToken cancellationToken)
    {
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
        
        _logger.LogInformation("Started {SagaType} saga {SagaId} for correlation {CorrelationId}",
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
                    "Step {StepName} marked saga as waiting for external event. " +
                    "Saga {SagaId} will be when event arrives.",
                    step.StepName, sagaId);

                sagaState.Status = SagaStatus.WaitingForEvent;
                await _sagaRepository.SaveAsync(sagaState, cancellationToken);
                
                // @think: should we create saga/sagaStep domain entity with 
                // additional logic for state changing type shit
                // UPD: We AGNI, but comment will remain in honor to anti-AI development

                return SagaResult.Success(sagaId);
            }
            
            if (!stepResult.Success)
            {
                _logger.LogWarning(
                    "Step {StepName} failed in saga {SagaId}. Starting compensation...",
                    step.StepName, sagaId);

                await CompensateAsync(sagaId, cancellationToken);
                return SagaResult.Failed(sagaId, stepResult.ErrorMessage ?? "Unknown error");
            }

            
            await _sagaRepository.SaveAsync(sagaState, cancellationToken);
        }

        sagaState.Status = SagaStatus.Completed;
        sagaState.UpdatedAt = DateTime.Now;
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
        var typedContext = (TContext)context;
        
       _logger.LogInformation(
           "Resuming {SagaType} for correlation {CorrelationId} from step {StepName}",
           SagaType, data.CorrelationId, fromStepName);

       var sagaState = await _sagaRepository.GetByCorrelationIdAsync(data.CorrelationId, SagaType, cancellationToken);

       if (sagaState == null)
       {
           _logger.LogError(
               "Cannot resume {SagaType} for {CorrelationId} - saga state not found",
               SagaType,
               data.CorrelationId);

           return SagaResult.Failed(Guid.Empty, "Saga state not found");
       }

       if (sagaState.Status != SagaStatus.WaitingForEvent)
       {
           _logger.LogWarning(
               "Saga {SagaId} is in status {Status}, expected WaitingForEvent. Cannot resume.",
               sagaState.Id,
               sagaState.Status);

           if (sagaState.Status == SagaStatus.Completed)
           {
               _logger.LogInformation(
                   "Saga {SagaId} already completed. This is likely a duplicate event/webhook.",
                   sagaState.Id);
               return SagaResult.Success(sagaState.Id);

           }

           return SagaResult.Failed(
                   sagaState.Id,
                   $"Saga not in waiting state: {sagaState.Status}");
       }

       sagaState.Status = SagaStatus.Running;
       sagaState.UpdatedAt = DateTime.UtcNow;
       sagaState.Context = JsonSerializer.Serialize(typedContext);
       await _sagaRepository.SaveAsync(sagaState, cancellationToken);


       var resumeStep = Steps.FirstOrDefault(s => s.StepName == fromStepName);

       if (resumeStep == null)
       {
           _logger.LogError(
               "Step {StepName} not found in {SagaType}",
               fromStepName,
               SagaType);

           return SagaResult.Failed(sagaState.Id, $"Step {fromStepName} not found");
       }

       var remainingSteps = Steps
           .Where(s => s.Order >= resumeStep.Order)
           .OrderBy(s => s.Order)
           .ToList();

       _logger.LogInformation(
           "Resuming saga {SagaId} with {StepCount} remaining steps",
           sagaState.Id,
           remainingSteps.Count);

       foreach (var step in remainingSteps)
       {
           _logger.LogInformation(
               "Executing step {StepName} (order: {Order}) for {Correlation}",
               step.StepName,
               step.Order,
               data.CorrelationId);

           var stepResult = await ExecuteStepAsync(
               sagaState.Id,
               step,
               data,
               typedContext,
               cancellationToken);
           
           sagaState.Context = JsonSerializer.Serialize(typedContext);
           sagaState.CurrentStep = step.StepName;
           sagaState.UpdatedAt = DateTime.UtcNow;
           
           // check if step marked saga as waiting again (for multi-webhook scenarios)
           if (stepResult.Metadata.TryGetValue("SagaState", out var sagaStateValue) &&
               sagaStateValue?.ToString() == "WaitingForEvent")
           {
               _logger.LogInformation(
                   "Step {StepName} marked saga as waiting for another external event.",
                   step.StepName);

               sagaState.Status = SagaStatus.WaitingForEvent;
               await _sagaRepository.SaveAsync(sagaState, cancellationToken);

               return SagaResult.Success(sagaState.Id);
           }

           if (!stepResult.Success)
           {
               _logger.LogError(
                   "Step {StepName} failed during resume for saga {SagaId}: {Error}",
                   step.StepName,
                   sagaState.Id,
                   stepResult.ErrorMessage);
               
               // run compensation
               await CompensateAsync(sagaState.Id, cancellationToken);
               return SagaResult.Failed(sagaState.Id, stepResult.ErrorMessage ?? "Unknown error");
           }

           await _sagaRepository.SaveAsync(sagaState, cancellationToken);
       }

       sagaState.Status = SagaStatus.Completed;
       sagaState.UpdatedAt = DateTime.UtcNow;
       await _sagaRepository.SaveAsync(sagaState, cancellationToken);

       _logger.LogInformation(
           "Saga {SagaId} resumed and completed successfully for {CorrelationId}",
           sagaState.Id,
           data.CorrelationId);

       return SagaResult.Success(sagaState.Id);


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

            await _sagaRepository.SaveStepAsync(stepLog, cancellationToken);

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

            await _sagaRepository.SaveStepAsync(stepLog, cancellationToken);

            return StepResult.Failure(ex.Message);
        }
    }

    public async Task<SagaResult> CompensateAsync(Guid sagaId, CancellationToken cancellationToken)
    {
        var sagaState = await _sagaRepository.GetByIdAsync(sagaId, cancellationToken);
        if (sagaState == null)
            throw new InvalidOperationException($"Saga {sagaId} not found");

        if (sagaState.Status == SagaStatus.Compensated)
        {
            _logger.LogInformation("Saga {SagaId} already compensated", sagaId);
            return SagaResult.Compensated(sagaId);
        }

        sagaState.Status = SagaStatus.Compensating;
        await _sagaRepository.SaveAsync(sagaState, cancellationToken);
        
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
                await _sagaRepository.SaveStepAsync(stepLog, cancellationToken);
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
        await _sagaRepository.SaveAsync(sagaState, cancellationToken);
            
         _logger.LogInformation("Saga {SagaId} compensated", sagaId);
        return SagaResult.Compensated(sagaId);
    }
}












































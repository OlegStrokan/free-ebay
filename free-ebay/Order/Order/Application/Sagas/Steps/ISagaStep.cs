namespace Application.Sagas.Steps;

public interface ISagaStep<TData, TContext>
    where TData : SagaData
    where TContext : SagaContext
{
    string StepName { get; }
    int Order { get; } // explicit order
    Task<StepOutcome> ExecuteAsync(TData data, TContext context, CancellationToken cancellationToken);
    Task CompensateAsync(TData data, TContext context, CancellationToken cancellationToken);
}

public abstract record StepOutcome;

public record Completed(Dictionary<string, object>? Data = null) : StepOutcome;

public record WaitForEvent : StepOutcome;

public record Fail(string Reason) : StepOutcome;
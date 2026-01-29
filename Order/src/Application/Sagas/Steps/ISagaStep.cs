namespace Application.Sagas.Steps;

public interface ISagaStep<TData, TContext>
    where TData : SagaData
    where TContext : SagaContext
{
    string StepName { get; }
    int Order { get;  } // explicit order
    Task<StepResult> ExecuteAsync(TData data, TContext context, CancellationToken cancellationToken);
    Task CompensateAsync(TData data, TContext context, CancellationToken cancellationToken);
}



public sealed class StepResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, object>? Data { get; }
    public Dictionary<string, object> Metadata { get; }

    private StepResult(
        bool success,
        string? errorMessage = null, 
        Dictionary<string, object>? data = null,
        Dictionary<string, object>? metadata = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
        Data = data ?? new Dictionary<string, object>();
        Metadata = metadata ?? new Dictionary<string, object>();

    }

    public static StepResult SuccessResult(Dictionary<string, object>? data = null)
        => new(true, null, data);

    public static StepResult Failure(string errorMessage)
        => new(false, errorMessage);
}
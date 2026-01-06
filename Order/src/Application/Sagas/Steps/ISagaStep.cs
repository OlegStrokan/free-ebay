namespace Application.Sagas.Steps;

public interface ISagaStep<TData>
{
    string StepName { get; }
    bool CanCompensate { get; }
    Task<StepResult> ExecuteAsync(TData data, SagaContext context, CancellationToken cancellationToken);
}



public sealed class StepResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, object>? Data { get; }

    private StepResult(bool success, string? errorMessage = null, Dictionary<string, object>? data = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
        Data = data ?? new Dictionary<string, object>();
    }

    public static StepResult SuccessResult(Dictionary<string, object>? data = null)
        => new(true, null, data);

    public static StepResult Failure(string errorMessage)
        => new(false, errorMessage);
}
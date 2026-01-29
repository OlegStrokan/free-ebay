namespace Application.Sagas;

public sealed class SagaResult
{
    public Guid SagaId { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public SagaStatus Status { get; }
    
    private SagaResult(
        Guid sagaId,
        bool isSuccess,
        SagaStatus status,
        string? errorMessage = null)
    {
        SagaId = sagaId;
        IsSuccess = isSuccess;
        Status = status;
        ErrorMessage = errorMessage;
    }
    
    public static SagaResult Success(Guid sagaId)
        => new(sagaId, true, SagaStatus.Completed);
    
    public static SagaResult Failed(Guid sagaId, string errorMessage)
        => new(sagaId, false, SagaStatus.Failed, errorMessage);
    
    public static SagaResult Compensated(Guid sagaId)
        => new(sagaId, false, SagaStatus.Compensated);
}

public enum SagaStatus
{
    Pending,
    Running,
    WaitingForEvent,
    Completed,
    Failed,
    Compensating,
    Compensated
}
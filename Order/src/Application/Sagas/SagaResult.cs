namespace Application.Sagas;

public sealed class SagaResult
{
   public Guid SagaId { get; }
   public bool IsSuccess { get; }
   public string? ErrorMessage { get; }
   public SagaContext? Context { get; }
   public SagaStatus Status { get; }

   private SagaResult(
       Guid sagaId,
       bool isSuccess,
       SagaStatus status,
       SagaContext? context = null,
       string? errorMessage = null)
   {
       SagaId = sagaId;
       IsSuccess = isSuccess;
       Status = status;
       Context = context;
       ErrorMessage = errorMessage;
   }

   public static SagaResult Success(Guid sagaId, SagaContext context)
       => new(sagaId, true, SagaStatus.Completed, context);

   public static SagaResult Failed(Guid sagaId, string errorMessage) 
       => new(sagaId, false, SagaStatus.Failed, null, errorMessage);

   public static SagaResult Compensated(Guid sagaId)
       => new(sagaId, false, SagaStatus.Compensated);
}

public enum SagaStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated
}
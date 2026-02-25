namespace Application.Sagas;

public interface ISagaBase<TData> 
    where TData : SagaData

{
    Task<SagaResult> ExecuteAsync(TData data, CancellationToken cancellationToken);

    Task<SagaResult> ResumeFromStepAsync(TData saga, SagaContext context, string fromStepName,
        CancellationToken cancellationToken);
    Task<SagaResult> CompensateAsync(Guid sagaId, CancellationToken cancellationToken);
}
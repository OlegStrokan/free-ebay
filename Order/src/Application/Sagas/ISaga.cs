namespace Application.Sagas;

public interface ISaga<TData> 
    where TData : SagaData

{
    Task<SagaResult> ExecuteAsync(TData data, CancellationToken cancellationToken);
    Task<SagaResult> CompensateAsync(Guid sagaId, CancellationToken cancellationToken);
}


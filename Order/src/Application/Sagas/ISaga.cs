namespace Application.Sagas;

public interface ISaga<TData>
{
    Task<SagaResult> ExecuteAsync(TData data, CancellationToken cancellationToken);
    Task<SagaResult> CompensateAsync(Guid sagaId, CancellationToken cancellationToken);
}

public interface IOrderSaga : ISaga<OrderSagaData>
{
    
}
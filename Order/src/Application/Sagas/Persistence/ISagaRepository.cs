namespace Application.Sagas.Persistence;

public interface ISagaRepository
{
    Task<SagaState?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken);
    Task<SagaState?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task SaveAsync(SagaState sagaState, CancellationToken cancellationToken);
    Task SaveStepAsync(SagaStepLog sagaState, CancellationToken cancellationToken);
    Task<List<SagaState>> GetStuckSagasAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
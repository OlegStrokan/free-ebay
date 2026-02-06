namespace Application.Sagas.Persistence;

public interface ISagaRepository
{
    Task<SagaState?> GetByIdAsync(
        Guid sagaId,
        CancellationToken cancellationToken
    );

    Task<SagaState?> GetByCorrelationIdAsync(
        Guid correlationId,
        string sagaType, CancellationToken
            cancellationToken);

    Task SaveAsync(
        SagaState sagaState,
        CancellationToken cancellationToken
    );

    Task SaveStepAsync(
        SagaStepLog stepLog,
        CancellationToken cancellationToken
    );

    Task<List<SagaState>> GetStuckSagasAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken
    );

    Task SaveCompensationStateAsync(
        SagaState sagaState,
        SagaStepLog stepLog,
        CancellationToken cancellationToken
    );
}

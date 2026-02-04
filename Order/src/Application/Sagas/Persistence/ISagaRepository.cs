namespace Application.Sagas.Persistence;

public interface ISagaRepository
{
    Task<SagaState<TContext>?> GetByIdAsync<TContext>(
        Guid sagaId,
        CancellationToken cancellationToken
        ) where TContext : SagaContext;

    Task<SagaState<TContext>?> GetByCorrelationIdAsync<TContext>(
        Guid correlationId,
        string sagaType, CancellationToken
            cancellationToken) where TContext : SagaContext;

    Task SaveAsync<TContext>(
        SagaState<TContext> sagaState,
        CancellationToken cancellationToken
    ) where TContext : SagaContext;
    
    Task SaveStepAsync(
        SagaStepLog stepLog,
        CancellationToken cancellationToken
        );

    Task<List<SagaState<TContext>>> GetStuckSagasAsync<TContext>(
        TimeSpan timeout,
        CancellationToken cancellationToken
    ) where TContext : SagaContext;
    
    Task SaveCompensationStateAsync<TContext>(
        SagaState<TContext> sagaState,
        SagaStepLog stepLog,
        CancellationToken cancellationToken) 
        where TContext : SagaContext;
}
using System.Text.Json;
using Application.Sagas.Persistence;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.Handlers;

public abstract class SagaEventHandler<TEvent, TData, TContext> : ISagaEventHandler
     where TData : SagaData
     where TContext : SagaContext, new()
{
    private readonly ISaga<TData> _saga;
    private readonly ISagaRepository _sagaRepository;
    private readonly ILogger _logger;
    
    public abstract string EventType { get;  }
    public abstract string SagaType { get;  }

    protected SagaEventHandler(
        ISaga<TData> saga,
        ISagaRepository sagaRepository,
        ILogger logger)
    {
        _saga = saga;
        _sagaRepository = sagaRepository;
        _logger = logger;
    }


    public async Task HandleAsync(string eventPayload, CancellationToken cancellationToken)
    {
        var eventDto = JsonSerializer.Deserialize<TEvent>(eventPayload);
        if (eventDto == null)
        {
            _logger.LogWarning("Failed to deserialize {EventType}", EventType);
            return;
        }

        var sagaData = MapEventToSagaData(eventDto);

        _logger.LogInformation(
            "Starting {SagaType} for correlation {CorrelationId}", SagaType, sagaData.CorrelationId
            );
        
        // check if saga already exists: idempotency
        var existingSaga = await _sagaRepository.GetByCorrelationIdAsync(
            sagaData.CorrelationId,
            SagaType,
            cancellationToken);

        if (existingSaga != null)
        {
            _logger.LogWarning(
                "{SagaType} already exists for {CorrelationId} with status {Status}. Skipping.",
                SagaType, sagaData.CorrelationId, existingSaga.Status);
            return;
        }

        try
        {
            var result = await _saga.ExecuteAsync(sagaData, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "{SagaType} completed successfully for correlation {CorrelationId}",
                    SagaType, sagaData.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "{SagaType} failed for correlation {CorrelationId}: {Error}",
                    SagaType, sagaData.CorrelationId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{SagaType} execution threw exception for correlation {CorrelationId}",
                SagaType, sagaData.CorrelationId);
        }

    }

    // will be implemented by specific handlers
    protected abstract TData MapEventToSagaData(TEvent eventDto);
}
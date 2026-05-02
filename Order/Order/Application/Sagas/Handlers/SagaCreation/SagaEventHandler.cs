using System.Text.Json;
using Application.Sagas.Persistence;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.Handlers.SagaCreation;

public abstract class SagaEventHandler<TEvent, TData, TContext> : ISagaEventHandler
     where TData : SagaData
     where TContext : SagaContext, new()
{
    private readonly ISagaBase<TData> _saga;
    private readonly ISagaRepository _sagaRepository;
    private readonly ILogger _logger;
    
    public abstract string EventType { get;  }
    public abstract string SagaType { get;  }

    protected SagaEventHandler(
        ISagaBase<TData> saga,
        ISagaRepository sagaRepository,
        ILogger logger)
    {
        _saga = saga;
        _sagaRepository = sagaRepository;
        _logger = logger;
    }


    public async Task HandleAsync(string eventPayload, CancellationToken cancellationToken)
    {
        TEvent? eventDto;
        try
        {
            eventDto = JsonSerializer.Deserialize<TEvent>(eventPayload);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to deserialize {EventType}. Invalid JSON format.", EventType);
            return;
        }
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
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            // The unique index on (CorrelationId, SagaType) rejected a concurrent duplicate
            // This is the expected idempotent-duplicate scenario, treat as success
            _logger.LogInformation(
                "{SagaType} saga already created for correlation {CorrelationId} (concurrent duplicate detected). Skipping.",
                SagaType, sagaData.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{SagaType} execution threw exception for correlation {CorrelationId}",
                SagaType, sagaData.CorrelationId);
        }

    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
      for (var current = ex; current != null; current = current.InnerException)
        {
            var msg = current.Message;
            if (msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("23505", StringComparison.Ordinal)) 
            {
                return true;
            }
        }
        return false;
    }

    // will be implemented by specific handlers
    protected abstract TData MapEventToSagaData(TEvent eventDto);
}
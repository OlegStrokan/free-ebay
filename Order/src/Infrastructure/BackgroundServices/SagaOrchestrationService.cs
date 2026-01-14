using System.Text.Json;
using Application.DTOs;
using Application.Sagas;
using Application.Sagas.Handlers;
using Application.Sagas.OrderSaga;
using Application.Sagas.Persistence;
using Confluent.Kafka;
using Domain.Events;
using Infrastructure.Messaging;


namespace Infrastructure.BackgroundServices;

public class SagaOrchestrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaOrchestrationService> _logger;
    private readonly IConsumer<string, string> _kafkaConsumer;

    public SagaOrchestrationService(
        IServiceProvider serviceProvider,
        ILogger<SagaOrchestrationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var kafkaBootstrapServers = configuration["Kafka.BootstrapServers"] ?? "localhost:9092";
        // @think why default is order.events?
        var kafkaTopic = configuration["Kafka:SagaTopic"] ?? "order.events";

        var config = new ConsumerConfig
        {
            GroupId = "saga-orchestration-group",
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        _kafkaConsumer = new ConsumerBuilder<string, string>(config).Build();
        _kafkaConsumer.Subscribe(kafkaTopic);
    }
        
        
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       _logger.LogInformation("SagaOrchestrationService started");

       while (!stoppingToken.IsCancellationRequested)
       {
           try
           {
               var consumeResult = _kafkaConsumer.Consume(stoppingToken);

               if (consumeResult?.Message?.Value == null)
                   continue;

               var messageValue = consumeResult.Message.Value;

               _logger.LogInformation(
                   "Received message from topic {Topic}, partition {Partition}, office {Offset}",
                   consumeResult.Topic,
                   consumeResult.Partition,
                   consumeResult.Offset
               );

               var eventWrapper = JsonSerializer.Deserialize<EventWrapper>(messageValue);

               if (eventWrapper == null)
               {
                   _logger.LogWarning(("Failed to deserialize event wrapper"));
                   _kafkaConsumer.Commit(consumeResult);
                   continue;
               }

               await ProcessEventAsync(eventWrapper, stoppingToken);

               _kafkaConsumer.Commit(consumeResult);

               _logger.LogInformation("Successfully processed and committed office {Offset}", consumeResult.Offset);
           }
           catch (ConsumeException ex)
           {
               _logger.LogError(ex, "Kafka consume error");
               await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error processing message");
               await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
           }
           finally
           {

               _kafkaConsumer.Close();
               _logger.LogInformation("SagaOrchestrationService stopped");
           }
       }
    }

    private async Task ProcessEventAsync(
        EventWrapper eventWrapper,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var handlers = scope.ServiceProvider.GetServices<ISagaEventHandler>();

        var handler = handlers.FirstOrDefault(h => h.EventType == eventWrapper.EventType);

        if (handler == null)
        {
            _logger.LogWarning(
                "No saga handler for event type {EventType}. Skipping.",
                eventWrapper.EventType);
            return;
        }
        
        _logger.LogInformation(
            "Processing {EventType} with {HandlerType}",
            eventWrapper.EventType,
            handler.GetType().Name);

        await handler.HandleAsync(eventWrapper.Payload, cancellationToken);
    }

    public override void Dispose()
    {
        _kafkaConsumer?.Dispose();
        base.Dispose();
    }
}
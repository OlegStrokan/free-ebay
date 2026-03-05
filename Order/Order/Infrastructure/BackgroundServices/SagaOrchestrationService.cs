using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Sagas.Handlers;
using Application.Sagas.Handlers.SagaCreation;
using Confluent.Kafka;
using Infrastructure.Messaging;


namespace Infrastructure.BackgroundServices;

// kafka has no auto-instrumentation build for opentelemetry so we traced it manually
public class SagaOrchestrationService(
    IServiceProvider serviceProvider,
    ILogger<SagaOrchestrationService> logger,
    IConsumer<string, string> kafkaConsumer,
    IConfiguration configuration)
    : BackgroundService
{
    private static readonly ActivitySource _activitySource = new("OrderService.Kafka");
    private readonly string _topic = configuration["Kafka:SagaTopic"] ?? "order.events";


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       logger.LogInformation("SagaOrchestrationService started");
       
       kafkaConsumer.Subscribe(_topic);
       logger.LogInformation("Subscribed to topic: {Topic}", _topic);
       
       try
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               try
               {
                   var consumeResult = kafkaConsumer.Consume(stoppingToken);

                   if (consumeResult?.Message?.Value == null)
                       continue;

                   var messageValue = consumeResult.Message.Value;

                   logger.LogInformation(
                       "Received message from topic {Topic}, partition {Partition}, offset {Offset}",
                       consumeResult.Topic,
                       consumeResult.Partition,
                       consumeResult.Offset
                   );

                   var eventWrapper = JsonSerializer.Deserialize<EventWrapper>(messageValue);

                   if (eventWrapper == null)
                   {
                       logger.LogWarning("Failed to deserialize event wrapper");
                       kafkaConsumer.Commit(consumeResult);
                       continue;
                   }

                   // restore trace context propagated from the publisher
                   Activity? activity = null;
                   var traceHeader = consumeResult.Message.Headers?
                       .FirstOrDefault(h => h.Key == "traceparent");
                   if (traceHeader != null)
                   {
                       var traceparent = Encoding.UTF8.GetString(traceHeader.GetValueBytes());
                       if (ActivityContext.TryParse(traceparent, null, out var parentCtx))
                           activity = _activitySource.StartActivity(
                               $"kafka.consume.{eventWrapper.EventType}",
                               ActivityKind.Consumer, parentCtx);
                   }

                   try
                   {
                       await ProcessEventAsync(eventWrapper, stoppingToken);
                   }
                   finally
                   {
                       activity?.Dispose();
                   }

                   kafkaConsumer.Commit(consumeResult);

                   logger.LogInformation("Successfully processed and committed offset {Offset}", consumeResult.Offset);
               }
               catch (ConsumeException ex)
               {
                   logger.LogError(ex, "Kafka consume error");
                   await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
               }
               catch (OperationCanceledException)
               {
                   // stoppingToken was cancelled - exit the loop cleanly
                   throw;
               }
               catch (Exception ex)
               {
                   logger.LogError(ex, "Error processing message");
                   await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
               }
           }
       }
       finally
       {
           kafkaConsumer.Unsubscribe();
           kafkaConsumer.Close();
           logger.LogInformation("SagaOrchestrationService stopped");
       }
    }

    private async Task ProcessEventAsync(
        EventWrapper eventWrapper,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ISagaHandlerFactory>();
        var handler = factory.GetHandler(scope.ServiceProvider, eventWrapper.EventType);
        
        if (handler == null)
        {
            logger.LogWarning(
                "No saga handler for event type {EventType}. Skipping.",
                eventWrapper.EventType);
            return;
        }
        
        logger.LogInformation(
            "Processing {EventType} with {HandlerType}",
            eventWrapper.EventType,
            handler.GetType().Name);

        await handler.HandleAsync(eventWrapper.Payload, cancellationToken);
    }

    public override void Dispose()
    {
        kafkaConsumer?.Dispose();
        base.Dispose();
    }
}
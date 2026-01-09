using System.Text.Json;
using Application.DTOs;
using Application.Sagas;
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
        ILogger<SagaOrchestrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var config = new ConsumerConfig
        {
            GroupId = "saga-orchestration-group",
            BootstrapServers = "localhost:9092",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        _kafkaConsumer = new ConsumerBuilder<string, string>(config).Build();
        _kafkaConsumer.Subscribe("order.events");
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

               if (eventWrapper.EventType == "OrderCreateEvent")
               {
                   var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEventDto>(
                       eventWrapper.Payload
                   );

                   if (orderCreatedEvent != null)
                   {
                       await HandleOrderCreatedEventAsync(orderCreatedEvent, stoppingToken);
                   }
               }

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

           _kafkaConsumer.Close();
           _logger.LogInformation("SagaOrchestrationService stopped");
       }
    }

    private async Task HandleOrderCreatedEventAsync(
        OrderCreatedEventDto eventDto,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderSaga = scope.ServiceProvider.GetRequiredService<IOrderSaga>();
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
        
        _logger.LogInformation("Starting saga for order {OrderId}", eventDto.OrderId);
        
        // check if saga already exists (idempotency)
        var existingSaga = await sagaRepository.GetByOrderIdAsync(eventDto.OrderId, cancellationToken);

        if (existingSaga != null)
        {
            _logger.LogWarning(
                "Saga already exists for order {OrderId}. Skipping duplicate execution.",
                eventDto.OrderId);

            return;
        }
        
        // build saga data
        var sagaData = new OrderSagaData
        {
            OrderId = eventDto.OrderId,
            CustomerId = eventDto.CustomerId,
            Items = eventDto.Items,
            TotalAmount = eventDto.TotalAmount,
            PaymentMethod = eventDto.PaymentMethod ?? "stripe",
            DeliveryAddress = eventDto.DeliveryAddress
        };

        try
        {
            var result = await orderSaga.ExecuteAsync(sagaData, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Saga completed successfully for order {OrderId}",
                    eventDto.OrderId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Saga failed for order {OrderId}: {Error}",
                    eventDto.OrderId,
                    result.ErrorMessage
                );
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Saga execution threw exception for order {OrderId} ",
                eventDto.OrderId
            );

            throw;
        }
    }

    public override void Dispose()
    {
        _kafkaConsumer?.Dispose();
        base.Dispose();
    }
}
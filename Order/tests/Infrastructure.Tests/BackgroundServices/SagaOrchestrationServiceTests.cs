using System.Reflection;
using System.Text.Json;
using Application.Sagas.Handlers;
using Confluent.Kafka;
using Infrastructure.BackgroundServices;
using Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Infrastructure.Tests.BackgroundServices;

public class SagaOrchestrationServiceTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScope _serviceScope = Substitute.For<IServiceScope>();
    private readonly ILogger<SagaOrchestrationService> _logger = Substitute.For<ILogger<SagaOrchestrationService>>();
    private readonly IConfiguration __configuration = Substitute.For<IConfiguration>();
    private readonly IConsumer<string, string> _kafkaConsumer = Substitute.For<IConsumer<string, string>>();
    private readonly ISagaEventHandler _sagaEventHandler = Substitute.For<ISagaEventHandler>();

    public SagaOrchestrationServiceTests()
    {
        _serviceProvider.GetService(typeof(IServiceScopeFactory))
            .Returns(Substitute.For<IServiceScopeFactory>());
        _serviceProvider.CreateScope().Returns(_serviceScope);
        _serviceScope.ServiceProvider.Returns(_serviceProvider);

        __configuration["Kafka:SagaTopic"].Returns("order.events");
        __configuration["Kafka:BootstrapServers"].Returns("localhost:9092");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessHandler_WhenValidMessageReceived()
    {
        var eventType = "OrderCreatedEvent";
        var payload = "{\"OrderId\":\"" + Guid.NewGuid() + "\"}";
        var wrapper = new EventWrapper { EventType = eventType, Payload = payload };
        var jsonMessage = JsonSerializer.Serialize(wrapper);

        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string>() { Value = jsonMessage },
            Topic = "order.events",
            Partition = 0,
            Offset = 100
        };

        var signal = new TaskCompletionSource<bool>();

        _kafkaConsumer
            .When(x => x.Commit((Arg.Is<ConsumeResult<string, string>>(r => r.Offset == 100))))
            .Do(_ => signal.TrySetResult(true));

        _kafkaConsumer.Consume(Arg.Any<CancellationToken>()).Returns(consumeResult);

        _serviceProvider.GetServices<ISagaEventHandler>().Returns(Enumerable.Empty<ISagaEventHandler>());

        var service = new TestableSagaOrchestrationService(_serviceProvider, _logger, __configuration, _kafkaConsumer);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        var completedTask = await Task.WhenAny(signal.Task, Task.Delay(5000, cts.Token));

        await cts.CancelAsync();
        
        Assert.True(completedTask == signal.Task, "Test Failed: Commit was never called within 2 seconds");

        await _sagaEventHandler.Received(1).HandleAsync(payload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCommitButSkip_WhenNoHandlersExists()
    {
        var wrapper = new EventWrapper { EventType = "UnknownEvent", Payload = "{}" };

        var consumerResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = JsonSerializer.Serialize(wrapper) }
        };

        var signal = new TaskCompletionSource<bool>();

        _kafkaConsumer
            .When(x => x.Commit(Arg.Any<ConsumeResult<string, string>>()))
            .Do(_ => signal.TrySetResult(true));

        _kafkaConsumer.Consume(Arg.Any<CancellationToken>()).Returns(consumerResult);

        _serviceProvider.GetServices<ISagaEventHandler>().Returns(Enumerable.Empty<ISagaEventHandler>());

        var service = new TestableSagaOrchestrationService(_serviceProvider, _logger, __configuration, _kafkaConsumer);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        var completedTask = await Task.WhenAny(signal.Task, Task.Delay(2000, cts.Token));
        await cts.CancelAsync();
            
        
        Assert.True(completedTask == signal.Task, "Test Failed: Service did not process/commit the unknown event.");

        await _sagaEventHandler.DidNotReceive().HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

// helper to inject the mock consumer
public class TestableSagaOrchestrationService : SagaOrchestrationService
{
    private readonly IConsumer<string, string> _mockConsumer;

    public TestableSagaOrchestrationService(
        IServiceProvider sp,
        ILogger<SagaOrchestrationService> logger,
        IConfiguration config,
        IConsumer<string, string> mockConsumer) : base(sp, logger, config)
    {
        var field = typeof(SagaOrchestrationService).GetField("_kafkaConsumer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(this, mockConsumer);
    }
}
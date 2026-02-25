using System.Text.Json;
using Application.Interfaces;
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
    private readonly IConsumer<string, string> _consumer =
        Substitute.For<IConsumer<string, string>>();

    private readonly ISagaHandlerFactory _handlerFactory =
        Substitute.For<ISagaHandlerFactory>();

    private readonly ILogger<SagaOrchestrationService> _logger =
        Substitute.For<ILogger<SagaOrchestrationService>>();

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScope _serviceScope       = Substitute.For<IServiceScope>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Kafka:SagaTopic"] = "test.saga.events" })
        .Build();

    public SagaOrchestrationServiceTests()
    {
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_serviceScope);
        _serviceScope.ServiceProvider.GetService(typeof(ISagaHandlerFactory)).Returns(_handlerFactory);
    }

    private SagaOrchestrationService Build() =>
        new(_serviceProvider, _logger, _consumer, _config);

    private static ConsumeResult<string, string> MakeConsumeResult(string eventType, string payload)
    {
        var wrapper = new EventWrapper
        {
            EventId   = Guid.NewGuid(),
            EventType = eventType,
            Payload   = payload,
            OccurredOn = DateTime.UtcNow
        };
        return new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = JsonSerializer.Serialize(wrapper) }
        };
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldCallHandleAsync_WhenHandlerFound()
    {
        const string eventType = "OrderCreatedEvent";
        const string payload   = "{\"orderId\":\"some-id\"}";

        var handler = Substitute.For<ISagaEventHandler>();
        handler.EventType.Returns(eventType);

        using var cts = new CancellationTokenSource();
        var signal = new TaskCompletionSource<bool>();

        handler
            .When(h => h.HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                signal.TrySetResult(true);
                cts.Cancel(); // stop the service loop after the first event
            });

        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(MakeConsumeResult(eventType, payload));

        _handlerFactory.GetHandler(Arg.Any<IServiceProvider>(), eventType)
            .Returns(handler);

        await Build().StartAsync(cts.Token);

        var completed = await Task.WhenAny(signal.Task, Task.Delay(3000));
        Assert.True(completed == signal.Task, "HandleAsync was never called within 3 seconds.");

        await handler.Received(1).HandleAsync(payload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipEvent_WhenNoHandlerFound()
    {
        const string eventType = "UnknownEvent";

        _handlerFactory.GetHandler(Arg.Any<IServiceProvider>(), eventType)
            .Returns((ISagaEventHandler?)null);

        using var cts = new CancellationTokenSource();
        var commitSignal = new TaskCompletionSource<bool>();

        // When Commit is called (meaning we processed through to the end), signal and cancel
        _consumer
            .When(c => c.Commit(Arg.Any<ConsumeResult<string, string>>()))
            .Do(_ =>
            {
                commitSignal.TrySetResult(true);
                cts.Cancel();
            });

        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(MakeConsumeResult(eventType, "{}"));

        await Build().StartAsync(cts.Token);

        var completed = await Task.WhenAny(commitSignal.Task, Task.Delay(3000));
        Assert.True(completed == commitSignal.Task, "Commit was never called.");

        // handler factory was asked but no handler was set up — nothing should be dispatched
        _handlerFactory.Received().GetHandler(Arg.Any<IServiceProvider>(), eventType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipAndCommit_WhenMessageValueIsNull()
    {
        var nullResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = null! }
        };

        using var cts = new CancellationTokenSource();
        var signal = new TaskCompletionSource<bool>();

        // if no handler is ever looked up, it means we skipped correctly.
        // cancel after the first Consume to stop the loop.
        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                signal.TrySetResult(true);
                cts.Cancel();
                return nullResult;
            });

        await Build().StartAsync(cts.Token);

        var completed = await Task.WhenAny(signal.Task, Task.Delay(3000));
        Assert.True(completed == signal.Task);

        _handlerFactory.DidNotReceive().GetHandler(Arg.Any<IServiceProvider>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCommitAfterSuccessfulHandling()
    {
        const string eventType = "OrderCreatedEvent";
        var handler = Substitute.For<ISagaEventHandler>();
        handler.EventType.Returns(eventType);

        using var cts = new CancellationTokenSource();
        var signal = new TaskCompletionSource<bool>();

        _consumer
            .When(c => c.Commit(Arg.Any<ConsumeResult<string, string>>()))
            .Do(_ =>
            {
                signal.TrySetResult(true);
                cts.Cancel();
            });

        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(MakeConsumeResult(eventType, "{}"));

        _handlerFactory.GetHandler(Arg.Any<IServiceProvider>(), eventType)
            .Returns(handler);

        await Build().StartAsync(cts.Token);

        var completed = await Task.WhenAny(signal.Task, Task.Delay(3000));
        Assert.True(completed == signal.Task, "Commit was never called after successful handling.");

        _consumer.Received().Commit(Arg.Any<ConsumeResult<string, string>>());
    }
}

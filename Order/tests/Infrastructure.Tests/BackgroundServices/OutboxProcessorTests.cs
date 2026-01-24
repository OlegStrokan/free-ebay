using Application.Interfaces;
using Application.Models;
using Domain.Events;
using Domain.Events.CreateOrder;
using Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.BackgroundServices;

public class OutboxProcessorTests
{
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly ILogger<OutboxProcessor> _logger = Substitute.For<ILogger<OutboxProcessor>>();

    // scoping mocks
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScope _serviceScope = Substitute.For<IServiceScope>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    
    public OutboxProcessorTests()
    {
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_serviceScope);
        _serviceScope.ServiceProvider.GetService(typeof(IOutboxRepository)).Returns(_outboxRepository);
    }

    /// <summary>
    /// test wires the switcher to MarkAsProcessedAsync
    /// worker.StartAsync launches the background loop
    /// the worker calls GetUnprocessedMessageAsync => gets 1 message
    /// action 1 (publish): PublishRawAsync is called
    /// action 2 (database): MarkAsProcessedAsync is called
    /// because of MarkAsProcessedAsync is hit, signal.TrySetResult(true) is triggered immediately
    /// Task.WhenAny sees the signal.Task is complete
    /// assertion, cleanup
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldPublishAndMarkAsProcessed_WhenMessagesExists()
    {
        var message = new OutboxMessage(Guid.NewGuid(), nameof(OrderCreatedEvent), "{}", DateTime.UtcNow);

        _outboxRepository.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message });

        var signal = new TaskCompletionSource<bool>();
        
        _outboxRepository
            .When(x => x.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        var worker = new OutboxProcessor(_serviceProvider, _eventPublisher, _logger);

        using var cts = new CancellationTokenSource();

        await worker.StartAsync((cts.Token));

        var completedTask = await Task.WhenAny(signal.Task, Task.Delay(2000, cts.Token));

        Assert.True(completedTask == signal.Task, "Test Failed: PublishRawAsync was never called within 2 seconds.");

        await cts.CancelAsync();
        
        await _eventPublisher.Received(1).PublishRawAsync(message.Id, Arg.Any<string>(), 
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// test wires the switcher to LogError
    /// worker.StartAsync launches the background loop
    /// the worker calls GetUnprocessedMessageAsync => gets 1 message
    /// the code enters catch block. it skips the MarkAsProcessedAsync (we will test it to)
    /// logger.LogError is called
    /// NSubstitute detects the log call and triggers signal.TrySetResult(true)
    /// test thread wakes up the moment the error is logged
    /// assertion (MarkAsProcessedAsync was never called)
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldNotMarkAsProcessed_WhenPublishingFails()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "BrokenEvent", "{}", DateTime.UtcNow);
        
        _outboxRepository.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message });

        _eventPublisher.PublishRawAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Kafka is down!"));

        var signal = new TaskCompletionSource<bool>();
        
        _logger
            .When(x => x.Log(
                LogLevel.Error,
                Arg.Any<EventId>(), 
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()))
            .Do(_ => signal.TrySetResult(true));

        var worker = new OutboxProcessor(_serviceProvider, _eventPublisher, _logger);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);

        var completedTask = await Task.WhenAny(signal.Task, Task.Delay(2000, cts.Token));

        await cts.CancelAsync();
        
        Assert.True(completedTask == signal.Task, "Test Failed: Exception was not logged.");
        
        await _outboxRepository.DidNotReceive().MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }   

}
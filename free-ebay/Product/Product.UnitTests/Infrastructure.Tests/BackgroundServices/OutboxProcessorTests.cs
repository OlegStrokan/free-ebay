using Application.Interfaces;
using Application.Models;
using Infrastructure.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.BackgroundServices;

[TestFixture]
public class OutboxProcessorTests
{
    private IEventPublisher _eventPublisher;
    private IOutboxRepository _outboxRepository;
    private ILogger<OutboxProcessor> _logger;
    private IConfiguration _configuration;
    private IServiceProvider _serviceProvider;
    private IServiceScope _serviceScope;
    private IServiceScopeFactory _scopeFactory;

    [SetUp]
    public void SetUp()
    {
        _eventPublisher = Substitute.For<IEventPublisher>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _logger = Substitute.For<ILogger<OutboxProcessor>>();

        // Fast poll interval so tests don't wait 2 seconds per loop
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:BatchSize"] = "20",
                ["Outbox:MaxRetries"] = "5",
                ["Outbox:PollIntervalMs"] = "50",
                ["Outbox:MaxParallelism"] = "5"
            })
            .Build();

        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceScope = Substitute.For<IServiceScope>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();

        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_serviceScope);
        _serviceScope.ServiceProvider.GetService(typeof(IOutboxRepository)).Returns(_outboxRepository);
    }

    [TearDown]
    public async Task TearDown()
    {
        _serviceScope.Dispose();
        await Task.Delay(50);
    }

    [Test]
    public async Task ExecuteAsync_WhenBatchIsEmpty_ShouldNotPublishAnything()
    {
        _outboxRepository
            .GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage>());

        using var cts = new CancellationTokenSource();
        var worker = CreateProcessor();

        await worker.StartAsync(cts.Token);
        await Task.Delay(200); // let at least one poll cycle run
        await cts.CancelAsync();

        await _eventPublisher.DidNotReceiveWithAnyArgs()
            .PublishRawAsync(default, default!, default!, default, default!, default);
    }

    [Test]
    public async Task ExecuteAsync_WhenMessageSucceeds_ShouldPublishAndMarkAsProcessed()
    {
        var message = MakeMessage("ProductCreatedEvent");

        _outboxRepository
            .GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message });

        var signal = new TaskCompletionSource<bool>();
        _outboxRepository
            .When(x => x.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        using var cts = new CancellationTokenSource();
        var worker = CreateProcessor();

        await worker.StartAsync(cts.Token);
        var completed = await Task.WhenAny(signal.Task, Task.Delay(3000));
        await cts.CancelAsync();

        Assert.That(completed, Is.EqualTo(signal.Task), "MarkAsProcessedAsync was not called within 3s");

        await _eventPublisher.Received(1).PublishRawAsync(
            message.Id, message.Type, message.Content,
            Arg.Any<DateTime>(), message.AggregateId, Arg.Any<CancellationToken>());
        await _outboxRepository.Received().MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenPublishFails_ShouldIncrementRetryCount()
    {
        var message = MakeMessage("ProductCreatedEvent");

        _outboxRepository
            .GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message });

        _eventPublisher
            .PublishRawAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Kafka down"));

        var signal = new TaskCompletionSource<bool>();
        _outboxRepository
            .When(x => x.IncrementRetryCountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        using var cts = new CancellationTokenSource();
        var worker = CreateProcessor();

        await worker.StartAsync(cts.Token);
        var completed = await Task.WhenAny(signal.Task, Task.Delay(3000));
        await cts.CancelAsync();

        Assert.That(completed, Is.EqualTo(signal.Task), "IncrementRetryCountAsync was not called within 3s");

        await _outboxRepository.Received().IncrementRetryCountAsync(
            message.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenPublishFails_ShouldNotMarkAsProcessed()
    {
        var message = MakeMessage("ProductCreatedEvent");

        _outboxRepository
            .GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message });

        _eventPublisher
            .PublishRawAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Kafka down"));

        var signal = new TaskCompletionSource<bool>();
        _outboxRepository
            .When(x => x.IncrementRetryCountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        using var cts = new CancellationTokenSource();
        var worker = CreateProcessor();

        await worker.StartAsync(cts.Token);
        await Task.WhenAny(signal.Task, Task.Delay(3000));
        await cts.CancelAsync();

        await _outboxRepository.DidNotReceive()
            .MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenRepositoryReturnsNoMessages_DueToMaxRetries_ShouldNotPublishOrMarkProcessed()
    {
        // Exhausted messages are filtered out by the repository (RetryCount < maxRetries).
        // This test verifies the processor handles an empty batch gracefully when
        // all pending messages are exhausted.
        _outboxRepository
            .GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage>());

        using var cts = new CancellationTokenSource();
        var worker = CreateProcessor();

        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();

        await _eventPublisher.DidNotReceiveWithAnyArgs()
            .PublishRawAsync(default, default!, default!, default, default!, default);
        await _outboxRepository.DidNotReceive()
            .MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WithMultipleMessagesForSameAggregate_ShouldProcessInOrder()
    {
        var aggregateId = Guid.NewGuid().ToString();
        var order = new List<string>();

        // Two messages from same aggregate - should process sequentially in OccurredOn order
        var msg1 = MakeMessage("ProductCreatedEvent", aggregateId: aggregateId,
            occurredOn: DateTime.UtcNow.AddMinutes(-2));
        var msg2 = MakeMessage("ProductUpdatedEvent", aggregateId: aggregateId,
            occurredOn: DateTime.UtcNow.AddMinutes(-1));

        _outboxRepository
            .GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { msg2, msg1 }); // reversed to validate ordering

        var processedCount = 0;
        var signal = new TaskCompletionSource<bool>();

        _outboxRepository
            .When(x => x.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                processedCount++;
                if (processedCount == 2)
                    signal.TrySetResult(true);
            });

        _eventPublisher
            .When(x => x.PublishRawAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci => order.Add(ci.ArgAt<string>(1)));

        using var cts = new CancellationTokenSource();
        var worker = CreateProcessor();

        await worker.StartAsync(cts.Token);
        await Task.WhenAny(signal.Task, Task.Delay(3000));
        await cts.CancelAsync();

        Assert.That(order, Has.Count.EqualTo(2));
        Assert.That(order[0], Is.EqualTo("ProductCreatedEvent"));
        Assert.That(order[1], Is.EqualTo("ProductUpdatedEvent"));
    }

    private OutboxProcessor CreateProcessor() =>
        new OutboxProcessor(_serviceProvider, _eventPublisher, _logger, _configuration);

    private static OutboxMessage MakeMessage(
        string type,
        int retryCount = 0,
        string? aggregateId = null,
        DateTime? occurredOn = null)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            type,
            "{}",
            occurredOn ?? DateTime.UtcNow,
            aggregateId ?? Guid.NewGuid().ToString());

        if (retryCount > 0)
            typeof(OutboxMessage)
                .GetProperty(nameof(OutboxMessage.RetryCount))!
                .SetValue(message, retryCount);

        return message;
    }
}

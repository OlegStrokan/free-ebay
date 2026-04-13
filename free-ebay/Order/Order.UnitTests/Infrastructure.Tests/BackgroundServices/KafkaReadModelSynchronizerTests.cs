using Application.Interfaces;
using Application.Models;
using Confluent.Kafka;
using Infrastructure.BackgroundServices;
using Infrastructure.Messaging;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net.Sockets;
using System.Text;

namespace Infrastructure.Tests.BackgroundServices;

/// <summary>
/// Unit tests for KafkaReadModelSynchronizer.
/// The real Kafka consumer is substituted; the service provider is wired
/// with NSubstitute so each scope resolves the right fakes.
/// </summary>
public class KafkaReadModelSynchronizerTests
{
    // ── fakes ──────────────────────────────────────────────────────────────
    private readonly IConsumer<string, string> _consumer =
        Substitute.For<IConsumer<string, string>>();

    private readonly IReadModelEventDispatcher _dispatcher =
        Substitute.For<IReadModelEventDispatcher>();

    private readonly IKafkaRetryRepository _retryRepo =
        Substitute.For<IKafkaRetryRepository>();

    private readonly ILogger<KafkaReadModelSynchronizer> _logger =
        Substitute.For<ILogger<KafkaReadModelSynchronizer>>();

    // ── scopes ────────────────────────────────────────────────────────────
    private readonly IServiceProvider _rootProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _scopedProvider = Substitute.For<IServiceProvider>();

    public KafkaReadModelSynchronizerTests()
    {
        // Scope wiring
        _rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_scopedProvider);

        _scopedProvider.GetService(typeof(IReadModelEventDispatcher)).Returns(_dispatcher);
        _scopedProvider.GetService(typeof(IKafkaRetryRepository)).Returns(_retryRepo);
        // AppDbContext is not registered — IsDbHealthyAsync will return false (catch block),
        // meaning paused partitions stay paused. That is acceptable in unit tests.
    }

    private KafkaReadModelSynchronizer Build()
    {
        var kafkaOptions = Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            OrderEventsTopic = "order.events",
            ReturnEventsTopic = "return.events"
        });
        // Zero-delay retries so tests don't wait 1s+2s between attempts
        TimeSpan[] noDelays = [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero];
        return new KafkaReadModelSynchronizer(_rootProvider, _logger, _consumer, kafkaOptions, noDelays);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static ConsumeResult<string, string> MakeResult(
        string eventType = "OrderCreated",
        string payload   = "{\"EventId\":\"00000000-0000-0000-0000-000000000001\"}",
        string? key      = "agg-1",
        int partition    = 0,
        int offset       = 0)
    {
        var headers = new Headers();
        headers.Add("event-type", Encoding.UTF8.GetBytes(eventType));

        return new ConsumeResult<string, string>
        {
            Topic     = "order.events",
            Partition = new Partition(partition),
            Offset    = new Offset(offset),
            Message   = new Message<string, string>
            {
                Key     = key!,
                Value   = payload,
                Headers = headers
            }
        };
    }

    private static async Task RunUntilSignalAsync(
        KafkaReadModelSynchronizer sut,
        TaskCompletionSource<bool> signal,
        string timeoutMessage,
        int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await sut.StartAsync(cts.Token);
            var completed = await Task.WhenAny(signal.Task, Task.Delay(timeoutMs));
            if (completed != signal.Task)
                Assert.Fail(timeoutMessage);
        }
        finally
        {
            await sut.StopAsync(CancellationToken.None);
        }
    }

    // ── tests ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ShouldCommitOffset_WhenDispatchSucceeds()
    {
        var result = MakeResult();
        var callCount = 0;

        _consumer.Consume(Arg.Any<TimeSpan>())
            .Returns(_ => callCount++ == 0 ? result : null);

        var signal = new TaskCompletionSource<bool>();
        _consumer
            .When(c => c.Commit(Arg.Any<ConsumeResult<string, string>>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Commit was not called after successful dispatch");

        _consumer.Received().StoreOffset(result);
        _consumer.Received().Commit(result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipAndCommit_WhenNoEventTypeHeader()
    {
        var msg = new ConsumeResult<string, string>
        {
            Topic     = "order.events",
            Partition = new Partition(0),
            Offset    = new Offset(0),
            Message   = new Message<string, string>
            {
                Key     = "agg",
                Value   = "{}",
                Headers = new Headers() // no event-type header
            }
        };

        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? msg : null);

        var signal = new TaskCompletionSource<bool>();
        _consumer.When(c => c.Commit(Arg.Any<ConsumeResult<string, string>>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Commit for headerless message not called");

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPausePartition_WhenSystemicFailureExhaustsRetries()
    {
        var result = MakeResult();
        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? result : null!);

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new SocketException()));

        var signal = new TaskCompletionSource<bool>();
        _consumer.When(c => c.Pause(Arg.Any<IEnumerable<TopicPartition>>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Partition was not paused on systemic failure");

        _consumer.DidNotReceive().Commit(Arg.Any<ConsumeResult<string, string>>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSeekBack_WhenSystemicFailure()
    {
        var result = MakeResult(partition: 0, offset: 42);
        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? result : null!);

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new SocketException()));

        var signal = new TaskCompletionSource<bool>();
        _consumer.When(c => c.Seek(Arg.Any<TopicPartitionOffset>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Seek was not called on systemic failure");

        _consumer.Received().Seek(new TopicPartitionOffset("order.events", 0, 42));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistRetryRecord_WhenMessageSpecificFailureExhaustsRetries()
    {
        var result = MakeResult();
        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? result : null!);

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("domain rule violated")));

        var signal = new TaskCompletionSource<bool>();
        _retryRepo
            .When(r => r.PersistAsync(Arg.Any<KafkaRetryRecord>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Retry record was not persisted");

        await _retryRepo.Received(1).PersistAsync(Arg.Any<KafkaRetryRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCommitOffset_WhenMessageSpecificFailurePersistsRetryRecord()
    {
        var result = MakeResult();
        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? result : null!);

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("bad payload")));

        var signal = new TaskCompletionSource<bool>();
        _consumer.When(c => c.Commit(Arg.Any<ConsumeResult<string, string>>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Offset not committed after message-specific retry record persist");

        _consumer.Received().Commit(result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCommit_WhenRetryRecordPersistenceFails()
    {
        var result = MakeResult();
        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? result : null!);

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("domain error")));

        _retryRepo.PersistAsync(Arg.Any<KafkaRetryRecord>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("DB write failed")));

        // Wait for Seek (the fallback when persist fails)
        var signal = new TaskCompletionSource<bool>();
        _consumer.When(c => c.Seek(Arg.Any<TopicPartitionOffset>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Seek not called after retry record persist failure");

        _consumer.DidNotReceive().Commit(Arg.Any<ConsumeResult<string, string>>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallDispatchThreeTimes_WhenAllImmediateRetriesFail()
    {
        var result = MakeResult();
        var callCount = 0;
        _consumer.Consume(Arg.Any<TimeSpan>()).Returns(_ => callCount++ == 0 ? result : null!);

        var dispatchCount = 0;
        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ =>
            {
                dispatchCount++;
                return Task.FromException<bool>(new InvalidOperationException("always fails"));
            });

        var signal = new TaskCompletionSource<bool>();
        _retryRepo.When(r => r.PersistAsync(Arg.Any<KafkaRetryRecord>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "No retry record persisted");

        Assert.Equal(3, dispatchCount);
    }
}

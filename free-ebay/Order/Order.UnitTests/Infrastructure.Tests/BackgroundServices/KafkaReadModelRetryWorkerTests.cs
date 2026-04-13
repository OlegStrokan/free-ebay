using Application.Interfaces;
using Application.Models;
using Infrastructure.BackgroundServices;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.BackgroundServices;

public class KafkaReadModelRetryWorkerTests
{
    // ── fakes ─────────────────────────────────────────────────────────────
    private readonly IKafkaRetryRepository _retryRepo = Substitute.For<IKafkaRetryRepository>();
    private readonly IDeadLetterRepository _dlqRepo = Substitute.For<IDeadLetterRepository>();
    private readonly IReadModelEventDispatcher _dispatcher = Substitute.For<IReadModelEventDispatcher>();
    private readonly ILogger<KafkaReadModelRetryWorker> _logger =
        Substitute.For<ILogger<KafkaReadModelRetryWorker>>();

    // ── scopes ────────────────────────────────────────────────────────────
    private readonly IServiceProvider _rootProvider = Substitute.For<IServiceProvider>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _scopedProvider = Substitute.For<IServiceProvider>();

    public KafkaReadModelRetryWorkerTests()
    {
        _rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_scopedProvider);

        _scopedProvider.GetService(typeof(IKafkaRetryRepository)).Returns(_retryRepo);
        _scopedProvider.GetService(typeof(IReadModelEventDispatcher)).Returns(_dispatcher);
        _scopedProvider.GetService(typeof(IDeadLetterRepository)).Returns(_dlqRepo);
    }

    private KafkaReadModelRetryWorker Build(int pollIntervalSeconds = 1, int retryLimit = 5, int batchSize = 20)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KafkaRetry:WorkerPollIntervalSeconds"] = pollIntervalSeconds.ToString(),
                ["KafkaRetry:WorkerRetryLimit"]          = retryLimit.ToString(),
                ["KafkaRetry:BatchSize"]                 = batchSize.ToString()
            })
            .Build();

        return new KafkaReadModelRetryWorker(_rootProvider, _logger, config);
    }

    private static KafkaRetryRecord MakeRecord(string eventType = "OrderCreated") =>
        KafkaRetryRecord.Create(
            eventId: Guid.NewGuid(),
            eventType: eventType,
            topic: "order.events",
            partition: 0,
            offset: 0,
            messageKey: "agg-1",
            payload: "{\"EventId\":\"00000000-0000-0000-0000-000000000001\"}",
            headers: null,
            correlationId: null,
            errorMessage: "previous failure",
            errorType: "System.Exception",
            nextRetryAt: DateTime.UtcNow.AddMinutes(-1));

    /// <summary>Advances a record to a specific RetryCount using domain methods.</summary>
    private static KafkaRetryRecord MakeRecordWithRetries(int retryCount)
    {
        var record = MakeRecord();
        for (var i = 0; i < retryCount; i++)
            record.Reschedule(i + 1, DateTime.UtcNow.AddMinutes(-1), "error", "System.Exception");
        return record;
    }

    private static async Task RunUntilSignalAsync(
        KafkaReadModelRetryWorker worker,
        TaskCompletionSource<bool> signal,
        string timeoutMessage,
        int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await worker.StartAsync(cts.Token);
            var completed = await Task.WhenAny(signal.Task, Task.Delay(timeoutMs));
            if (completed != signal.Task)
                Assert.Fail(timeoutMessage);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    // ── tests ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessDueRecords_ShouldMarkInProgressThenSucceeded_WhenDispatchSucceeds()
    {
        var record = MakeRecord();
        _retryRepo.GetDueRecordsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KafkaRetryRecord> { record }, new List<KafkaRetryRecord>());

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var signal = new TaskCompletionSource<bool>();
        _retryRepo.When(r => r.MarkSucceededAsync(record.Id, Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "MarkSucceededAsync was not called");

        await _retryRepo.Received(1).MarkInProgressAsync(record.Id, Arg.Any<CancellationToken>());
        await _retryRepo.Received(1).MarkSucceededAsync(record.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRecords_ShouldRescheduleRecord_WhenDispatchFailsBeforeLimit()
    {
        var record = MakeRecord(); // RetryCount starts at 0
        _retryRepo.GetDueRecordsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KafkaRetryRecord> { record }, new List<KafkaRetryRecord>());

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("domain error")));

        var signal = new TaskCompletionSource<bool>();
        _retryRepo.When(r => r.RescheduleAsync(
                record.Id, Arg.Any<int>(), Arg.Any<DateTime>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(retryLimit: 5), signal, "RescheduleAsync was not called");

        // RetryCount was 0, so newRetryCount = 1 (not yet at limit 5)
        await _retryRepo.Received(1).RescheduleAsync(
            record.Id, 1, Arg.Any<DateTime>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRecords_ShouldMarkDeadLetter_WhenRetryLimitReached()
    {
        // RetryCount = 4 so next attempt = 5 which equals limit
        var record = MakeRecordWithRetries(retryCount: 4);
        _retryRepo.GetDueRecordsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KafkaRetryRecord> { record }, new List<KafkaRetryRecord>());

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("still failing")));

        var signal = new TaskCompletionSource<bool>();
        _retryRepo.When(r => r.MarkDeadLetterAsync(
                record.Id, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(retryLimit: 5), signal, "MarkDeadLetterAsync was not called");

        await _retryRepo.Received(1).MarkDeadLetterAsync(
            record.Id, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _retryRepo.DidNotReceive().RescheduleAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<DateTime>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRecords_ShouldWriteToDeadLetterRepository_WhenRetryLimitReached()
    {
        var record = MakeRecordWithRetries(retryCount: 4);
        _retryRepo.GetDueRecordsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KafkaRetryRecord> { record }, new List<KafkaRetryRecord>());

        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("still failing")));

        var signal = new TaskCompletionSource<bool>();
        _dlqRepo.When(r => r.AddAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(retryLimit: 5), signal, "DLQ AddAsync was not called");

        await _dlqRepo.Received(1).AddAsync(
            Arg.Any<Guid>(),
            "KafkaReadModelRetryExhausted",
            record.Payload,
            record.FirstFailureTime,
            Arg.Any<string>(),
            5,
            record.MessageKey!,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRecords_ShouldDoNothing_WhenNoDueRecords()
    {
        _retryRepo.GetDueRecordsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KafkaRetryRecord>());

        using var cts = new CancellationTokenSource(500);
        var worker = Build(pollIntervalSeconds: 60); // long poll so it only runs once
        await worker.StartAsync(cts.Token);
        await Task.Delay(300);
        await worker.StopAsync(CancellationToken.None);

        await _dispatcher.DidNotReceiveWithAnyArgs()
            .DispatchAsync(default!, default!, default!, default);
        await _retryRepo.DidNotReceive()
            .MarkSucceededAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRecords_ShouldContinueWithNextRecord_WhenOneRecordFails()
    {
        var failing = MakeRecord(eventType: "OrderCreated");
        var succeeding = MakeRecord(eventType: "OrderPaid");

        _retryRepo.GetDueRecordsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KafkaRetryRecord> { failing, succeeding }, new List<KafkaRetryRecord>());

        var dispatchCount = 0;
        _dispatcher.DispatchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(ci =>
            {
                if (++dispatchCount == 1)
                    return Task.FromException<bool>(new InvalidOperationException("first record fails"));
                return Task.FromResult(true);
            });

        var signal = new TaskCompletionSource<bool>();
        _retryRepo.When(r => r.MarkSucceededAsync(succeeding.Id, Arg.Any<CancellationToken>()))
            .Do(_ => signal.TrySetResult(true));

        await RunUntilSignalAsync(Build(), signal, "Second record was not processed after first failed");

        await _retryRepo.Received(1).MarkSucceededAsync(succeeding.Id, Arg.Any<CancellationToken>());
    }
}

using System.Text.Json;
using Application.Consumers;
using Application.RetryStore;
using Confluent.Kafka;
using Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.Kafka;

[TestFixture]
public class KafkaConsumerRetryTests
{
    private IServiceProvider _serviceProvider;
    private IServiceScope _serviceScope;
    private IProductEventConsumer _consumer;
    private IRetryStore _retryStore;
    private ILogger<KafkaConsumerBackgroundService> _logger;
    private KafkaConsumerBackgroundService _sut;

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceScope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(_serviceScope);

        _consumer = Substitute.For<IProductEventConsumer>();
        _consumer.EventType.Returns("ProductCreatedEvent");

        _retryStore = Substitute.For<IRetryStore>();

        _serviceScope.ServiceProvider
            .GetService(typeof(IEnumerable<IProductEventConsumer>))
            .Returns(new[] { _consumer }.AsEnumerable());

        _serviceScope.ServiceProvider
            .GetService(typeof(IRetryStore))
            .Returns(_retryStore);

        _logger = Substitute.For<ILogger<KafkaConsumerBackgroundService>>();

        var options = Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            ConsumerGroupId = "test-group",
            ProductEventsTopic = "product.events",
        });

        _sut = new KafkaConsumerBackgroundService(options, _serviceProvider, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceScope.Dispose();
        _sut.Dispose();
    }

    #region TryProjectWithRetries

    [Test]
    public async Task TryProjectWithRetries_WhenFirstAttemptSucceeds_ShouldReturnTrue()
    {
        var wrapper = BuildWrapper();

        var result = await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TryProjectWithRetries_WhenFirstAttemptSucceeds_ShouldCallDispatchOnce()
    {
        var wrapper = BuildWrapper();

        await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        await _consumer.Received(1).ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TryProjectWithRetries_WhenFirstFailsSecondSucceeds_ShouldReturnTrue()
    {
        var wrapper = BuildWrapper();

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException(new InvalidOperationException("fail 1")),
                _ => Task.CompletedTask);

        var result = await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TryProjectWithRetries_WhenFirstFailsSecondSucceeds_ShouldCallDispatchTwice()
    {
        var wrapper = BuildWrapper();

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException(new InvalidOperationException("fail")),
                _ => Task.CompletedTask);

        await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        await _consumer.Received(2).ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TryProjectWithRetries_WhenFirstTwoFailThirdSucceeds_ShouldReturnTrue()
    {
        var wrapper = BuildWrapper();

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException(new InvalidOperationException("fail 1")),
                _ => Task.FromException(new InvalidOperationException("fail 2")),
                _ => Task.CompletedTask);

        var result = await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TryProjectWithRetries_WhenAllThreeAttemptsFail_ShouldReturnFalse()
    {
        var wrapper = BuildWrapper();

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("permanent fail"));

        var result = await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TryProjectWithRetries_WhenAllThreeAttemptsFail_ShouldCallDispatchThreeTimes()
    {
        var wrapper = BuildWrapper();

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("fail"));

        await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        await _consumer.Received(3).ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TryProjectWithRetries_WhenAllAttemptsFail_ShouldStoreLastException()
    {
        var wrapper = BuildWrapper();
        var finalException = new InvalidOperationException("final error");

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException(new InvalidOperationException("err 1")),
                _ => Task.FromException(new InvalidOperationException("err 2")),
                _ => Task.FromException(finalException));

        await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        Assert.That(_sut._lastProjectionException, Is.SameAs(finalException));
    }

    [Test]
    public async Task TryProjectWithRetries_WhenSucceeds_ShouldClearLastException()
    {
        var wrapper = BuildWrapper();

        await _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, CancellationToken.None);

        Assert.That(_sut._lastProjectionException, Is.Null);
    }

    [Test]
    public void TryProjectWithRetries_WhenCancelled_ShouldPropagateOperationCanceled()
    {
        var wrapper = BuildWrapper();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _consumer.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.TryProjectWithRetries("ProductCreatedEvent", wrapper, cts.Token));
    }

    #endregion

    #region HandleExhaustedRetries — systemic failure

    [Test]
    public async Task HandleExhaustedRetries_SystemicFailure_ShouldSeekBack()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        // Set up a systemic exception
        _sut._lastProjectionException = new HttpRequestException("connection refused");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.Received(1).Seek(Arg.Is<TopicPartitionOffset>(
            tpo => tpo.Partition.Value == 0 && tpo.Offset.Value == 42));
    }

    [Test]
    public async Task HandleExhaustedRetries_SystemicFailure_ShouldPausePartition()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new HttpRequestException("connection refused");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.Received(1).Pause(Arg.Any<IEnumerable<TopicPartition>>());
    }

    [Test]
    public async Task HandleExhaustedRetries_SystemicFailure_ShouldNotCommitOffset()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new HttpRequestException("connection refused");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.DidNotReceiveWithAnyArgs().StoreOffset(default(ConsumeResult<string, string>)!);
        consumer.DidNotReceiveWithAnyArgs().Commit(default(ConsumeResult<string, string>)!);
    }

    [Test]
    public async Task HandleExhaustedRetries_SystemicFailure_ShouldNotPersistRetryRecord()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new HttpRequestException("connection refused");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        await _retryStore.DidNotReceiveWithAnyArgs().PersistAsync(default!, default);
    }

    #endregion

    #region HandleExhaustedRetries — message-specific failure

    [Test]
    public async Task HandleExhaustedRetries_MessageSpecific_WhenPersistSucceeds_ShouldCommitOffset()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new Infrastructure.Elasticsearch.ElasticsearchIndexingException(
            "mapper_parsing_exception");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.Received(1).StoreOffset(result);
        consumer.Received(1).Commit(result);
    }

    [Test]
    public async Task HandleExhaustedRetries_MessageSpecific_ShouldPersistRetryRecord()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new Infrastructure.Elasticsearch.ElasticsearchIndexingException(
            "mapper_parsing_exception");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        await _retryStore.Received(1).PersistAsync(
            Arg.Is<RetryRecord>(r =>
                r.EventType == "ProductCreatedEvent" &&
                r.Topic == "product.events" &&
                r.Partition == 0 &&
                r.Offset == 42 &&
                r.Status == RetryRecordStatus.Pending &&
                r.RetryCount == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleExhaustedRetries_MessageSpecific_ShouldRecordErrorInfo()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new Infrastructure.Elasticsearch.ElasticsearchIndexingException(
            "mapper_parsing_exception");

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        await _retryStore.Received(1).PersistAsync(
            Arg.Is<RetryRecord>(r =>
                r.LastErrorMessage!.Contains("mapper_parsing_exception") &&
                r.LastErrorType!.Contains("ElasticsearchIndexingException")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleExhaustedRetries_MessageSpecific_WhenPersistFails_ShouldNotCommitOffset()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new Infrastructure.Elasticsearch.ElasticsearchIndexingException(
            "mapper_parsing_exception");

        _retryStore.PersistAsync(Arg.Any<RetryRecord>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB connection failed"));

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.DidNotReceiveWithAnyArgs().StoreOffset(default(ConsumeResult<string, string>)!);
        consumer.DidNotReceiveWithAnyArgs().Commit(default(ConsumeResult<string, string>)!);
    }

    [Test]
    public async Task HandleExhaustedRetries_MessageSpecific_WhenPersistFails_ShouldSeekBack()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result = BuildConsumeResult("product.events", 0, 42);

        _sut._lastProjectionException = new Infrastructure.Elasticsearch.ElasticsearchIndexingException(
            "mapper_parsing_exception");

        _retryStore.PersistAsync(Arg.Any<RetryRecord>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB connection failed"));

        await _sut.HandleExhaustedRetries(consumer, result, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.Received(1).Seek(Arg.Is<TopicPartitionOffset>(
            tpo => tpo.Partition.Value == 0 && tpo.Offset.Value == 42));
    }

    #endregion

    #region HandleExhaustedRetries — repeated partition pause is idempotent

    [Test]
    public async Task HandleExhaustedRetries_SystemicFailure_SamePartitionTwice_ShouldPauseOnlyOnce()
    {
        var consumer = Substitute.For<IConsumer<string, string>>();
        var result1 = BuildConsumeResult("product.events", 0, 42);
        var result2 = BuildConsumeResult("product.events", 0, 43);

        _sut._lastProjectionException = new HttpRequestException("connection refused");

        await _sut.HandleExhaustedRetries(consumer, result1, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);
        await _sut.HandleExhaustedRetries(consumer, result2, "ProductCreatedEvent", BuildWrapper(), CancellationToken.None);

        consumer.Received(1).Pause(Arg.Any<IEnumerable<TopicPartition>>());
    }

    #endregion

    // ------- Helpers -------

    private static EventWrapper BuildWrapper() => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "ProductCreatedEvent",
        Payload = JsonDocument.Parse("{}").RootElement,
        OccurredOn = DateTime.UtcNow,
    };

    private static ConsumeResult<string, string> BuildConsumeResult(
        string topic, int partition, long offset)
    {
        return new ConsumeResult<string, string>
        {
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, string>
            {
                Key = "key-1",
                Value = JsonSerializer.Serialize(new EventWrapper
                {
                    EventId = Guid.NewGuid(),
                    EventType = "ProductCreatedEvent",
                    Payload = JsonDocument.Parse("{}").RootElement,
                    OccurredOn = DateTime.UtcNow,
                }),
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes("ProductCreatedEvent") },
                },
            },
        };
    }
}

using System.Text.Json;
using Application.Sagas;
using Application.Sagas.Handlers;
using Application.Sagas.Handlers.SagaCreation;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas;

public class SagaEventHandlerTests
{
    private readonly ISagaRepository _repository = Substitute.For<ISagaRepository>();
    private readonly ISagaBase<TestSagaData> _sagaBase = Substitute.For<ISagaBase<TestSagaData>>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly TestSagaHandler _sut;

    public SagaEventHandlerTests()
    {
        _sut = new TestSagaHandler(_sagaBase, _repository, _logger);
    }
    
    [Fact]
    public async Task HandleAsync_ShouldStartSaga_WhenSagaDoesNotExist()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Value = "Test" };
        var payload = JsonSerializer.Serialize(testEvent);

        _repository
            .GetByCorrelationIdAsync(testEvent.Id, "TestSaga", Arg.Any<CancellationToken>())
            .Returns((SagaState?)null);

        _sagaBase
            .ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>())
            .Returns(SagaResult.Success(Guid.NewGuid()));

        await _sut.HandleAsync(payload, CancellationToken.None);

        await _sagaBase.Received(1).ExecuteAsync(
            Arg.Is<TestSagaData>(d => d.CorrelationId == testEvent.Id && d.SomeData == "Test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSkip_WhenSagaAlreadyRunning_Idempotency()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(testEvent);

        _repository
            .GetByCorrelationIdAsync(testEvent.Id, "TestSaga", Arg.Any<CancellationToken>())
            .Returns(new SagaState { Status = SagaStatus.Running });

        await _sut.HandleAsync(payload, CancellationToken.None);

        await _sagaBase.DidNotReceive().ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSkip_WhenSagaAlreadyCompleted_Idempotency()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(testEvent);

        _repository
            .GetByCorrelationIdAsync(testEvent.Id, "TestSaga", Arg.Any<CancellationToken>())
            .Returns(new SagaState { Status = SagaStatus.Completed });

        await _sut.HandleAsync(payload, CancellationToken.None);

        await _sagaBase.DidNotReceive().ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLogWarning_WhenDeserializationFails()
    {
        await _sut.HandleAsync("this is not json", CancellationToken.None);

        await _sagaBase.DidNotReceive().ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLogWarning_WhenSagaExecutionReturnsFailed()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(testEvent);

        _repository
            .GetByCorrelationIdAsync(testEvent.Id, "TestSaga", Arg.Any<CancellationToken>())
            .Returns((SagaState?)null);

        _sagaBase
            .ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>())
            .Returns(SagaResult.Failed(Guid.NewGuid(), "step blew up"));

        await _sut.HandleAsync(payload, CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCatchAndLogError_WhenSagaExecutionThrows()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(testEvent);
        var expectedException = new Exception("saga exploded");

        _repository
            .GetByCorrelationIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SagaState?)null);

        _sagaBase
            .ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        // must NOT propagate
        var exception = await Record.ExceptionAsync(() => _sut.HandleAsync(payload, CancellationToken.None));

        Assert.Null(exception);

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    // helpers
    public class TestEvent
    {
        public Guid Id { get; set; }
        public string? Value { get; set; }
    }

    public class TestSagaData : SagaData
    {
        public string? SomeData { get; set; }
    }

    public class TestSagaContext : SagaContext {}

    public class TestSagaHandler : SagaEventHandler<TestEvent, TestSagaData, TestSagaContext>
    {
        public override string EventType => "TestEvent";
        public override string SagaType => "TestSaga";

        public TestSagaHandler(ISagaBase<TestSagaData> sagaBase, ISagaRepository repo, ILogger logger)
            : base(sagaBase, repo, logger) {}

        protected override TestSagaData MapEventToSagaData(TestEvent eventDto) =>
            new() { CorrelationId = eventDto.Id, SomeData = eventDto.Value };
    }
}
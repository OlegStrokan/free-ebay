using System.Text.Json;
using Application.Sagas;
using Application.Sagas.Handlers;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas;

public class SagaEventHandlerTests
{
    public readonly ISagaRepository _repository;
    public readonly ISaga<TestSagaData> _saga;
    public readonly ILogger _logger;
    public readonly TestSagaHandler _sut; // system under test 


    public SagaEventHandlerTests()
    {
        _saga = Substitute.For<ISaga<TestSagaData>>();
        _repository = Substitute.For<ISagaRepository>();
        _logger = Substitute.For<ILogger>();

        _sut = new TestSagaHandler(_saga, _repository, _logger);
    }

    [Fact]
    public async Task HandleAsync_ShouldStartSaga_WhenSagaDoesNotExists()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Value = "Test" };
        var payload = JsonSerializer.Serialize(testEvent);

        _repository
            .GetByCorrelationIdAsync(testEvent.Id, "TestSaga", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SagaState?>(null));

        _saga
            .ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>())
            .Returns(SagaResult.Success(Guid.NewGuid()));

        await _sut.HandleAsync(payload, CancellationToken.None);

        await _saga.Received(1).ExecuteAsync(
            Arg.Is<TestSagaData>(d => d.CorrelationId == testEvent.Id && d.SomeData == "Test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSkip_WhenSagaAlreadyExists_Idempotency()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(testEvent);

        var existingSaga = new SagaState { Status = SagaStatus.Running };
        _repository
            .GetByCorrelationIdAsync(testEvent.Id, "TestSaga", Arg.Any<CancellationToken>())
            .Returns(existingSaga);

        await _sut.HandleAsync(payload, CancellationToken.None);

        await _saga.DidNotReceive().ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLogWarning_WhenDeserializationFails()
    {
        var invalidJson = "this is not json";

        await _sut.HandleAsync(invalidJson, CancellationToken.None);

        await _saga.DidNotReceive().ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
        
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCatchAndLogException_WhenSagaExecutionThrows()
    {
        var testEvent = new TestEvent { Id = Guid.NewGuid() };
        var payload = JsonSerializer.Serialize(testEvent);
        var expectedException = new Exception();

        _repository.GetByCorrelationIdAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SagaState?>(null));

        _saga.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>()).ThrowsAsync(expectedException);

        var exception = await Record.ExceptionAsync(async () => await _sut.HandleAsync(payload, CancellationToken.None));
        
        Assert.Null(exception);
        
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("execution threw exception")),
            expectedException,
            Arg.Any<Func<object, Exception, string>>());
    }
    

    public class TestEvent
    {
        public Guid Id { get; set; } 
        public string Value { get; set; }
    }

    public class TestSagaData : SagaData
    {
        public string SomeData { get; set; }
    }
    
    public class TestSagaContext : SagaContext {}


    public class TestSagaHandler : SagaEventHandler<TestEvent, TestSagaData, TestSagaContext>
    {
        public override string EventType => "TestEvent";
        public override string SagaType => "TestSaga";
        
        public TestSagaHandler(ISaga<TestSagaData> saga, ISagaRepository repo, ILogger logger) 
            : base(saga, repo, logger) {}

        protected override TestSagaData MapEventToSagaData(TestEvent eventDto)
        {
            return new TestSagaData
            {
                CorrelationId = eventDto.Id,
                SomeData = eventDto.Value
            };
        }
    }
}
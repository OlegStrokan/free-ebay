using System.Text.Json;
using Application.Sagas;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Sagas;

public class SagaBaseTests
{
    public readonly ISagaRepository _repository = Substitute.For<ISagaRepository>();
    public readonly ILogger _logger = Substitute.For<ILogger>();
    
    // mock the steps
    public readonly ISagaStep<TestSagaData, TestSagaContext> _step1 =
        Substitute.For<ISagaStep<TestSagaData, TestSagaContext>>();

    public readonly ISagaStep<TestSagaData, TestSagaContext> _step2 =
        Substitute.For<ISagaStep<TestSagaData, TestSagaContext>>();

    public readonly ISagaStep<TestSagaData, TestSagaContext> _step3 =
        Substitute.For<ISagaStep<TestSagaData, TestSagaContext>>();

    public class TestSaga : SagaBase<TestSagaData, TestSagaContext>
    {
        public TestSaga(ISagaRepository repo, IEnumerable<ISagaStep<TestSagaData, TestSagaContext>> steps,
            ILogger logger)
            : base(repo, steps, logger) {}

        protected override string SagaType => "TestSaga";
    }
    
    public class TestSagaData : SagaData {}

    public class TestSagaContext : SagaContext {}

    [Fact]
    public async Task ExecuteAsync_ShouldRunAllSteps_AndComplete_WhenAllStepsSucceed()
    {
        _step1.Order.Returns(1);
        _step1.StepName.Returns("Step1");
        _step1.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        _step2.Order.Returns(2);
        _step2.StepName.Returns("Step2");
        _step2.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        var saga = new TestSaga(_repository, [_step1, _step2], _logger);
        var data = new TestSagaData { CorrelationId = Guid.NewGuid() };

        var result = await saga.ExecuteAsync(data, CancellationToken.None);
        
        Assert.True(result.IsSuccess);

        await _step1.Received(1).ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());

        await _step2.Received(1).ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());

        await _repository.Received().SaveAsync(
            Arg.Is<SagaState>(s => s.Status == SagaStatus.Completed && s.CurrentStep == "Step2"),
            Arg.Any<CancellationToken>());

    }


    [Fact]
    public async Task ExecuteAsync_ShouldPauseSaga_WhenStepReturnWaitingForEventMetadata()
    {
        _step1.Order.Returns(1);
        _step1.StepName.Returns("Step1");
        _step1.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        _step2.Order.Returns(2);
        _step2.StepName.Returns("Step2");
        _step2.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult(new Dictionary<string, object>
            {
                ["SagaState"] = "WaitingForEvent"
            }));

        var saga = new TestSaga(_repository, new[] { _step2, _step2 }, _logger);
        var data = new TestSagaData { CorrelationId = Guid.NewGuid() };

        var result = await saga.ExecuteAsync(data, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SagaStatus.Completed, result.Status);

        await _repository.Received().SaveAsync(
            Arg.Is<SagaState>(s =>
                s.Status == SagaStatus.WaitingForEvent &&
                s.CurrentStep == "Step2"),
            Arg.Any<CancellationToken>());

        await _step3.DidNotReceive().ExecuteAsync(
            Arg.Any<TestSagaData>(),
            Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task ExecuteAsync_ShouldCompensate_WhenStepFails()
    {
        _step1.Order.Returns(1);
        _step1.StepName.Returns("Step1");
        _step2.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        _step2.Order.Returns(2);
        _step2.StepName.Returns("Step2");
        _step2.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.Failure("Step2 failed"));

        var sagaId = Guid.NewGuid();
        var sagaState = new SagaState
        {
            Id = sagaId,
            Status = SagaStatus.Running,
            Payload = JsonSerializer.Serialize(new TestSagaData()),
            Context = JsonSerializer.Serialize(new TestSagaContext()),
            Steps = new List<SagaStepLog>
            {
                new() { StepName = "Step1", Status = StepStatus.Completed }
            }
        };

        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(sagaState);

        var saga = new TestSaga(_repository, new[] { _step1, _step2 }, _logger);
        var data = new TestSagaData { CorrelationId = Guid.NewGuid() };

        var result = await saga.ExecuteAsync(data, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SagaStatus.Failed, result.Status);
        Assert.Equal("Step2 failed", result.ErrorMessage);

        // step1 should be compensated
        await _step1.Received(1).CompensateAsync(
            Arg.Any<TestSagaData>(),
            Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());
        
        // step2 shouldn't be compensated
        await _step2.DidNotReceive().CompensateAsync(
            Arg.Any<TestSagaData>(),
            Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveProgress_AfterEachStep()
    {
        _step1.Order.Returns(1);
        _step1.StepName.Returns("Step1");
        _step1.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        _step2.Order.Returns(2);
        _step2.StepName.Returns("Step2");
        _step2.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        var saga = new TestSaga(_repository, new[] { _step1, _step2 }, _logger);
        var data = new TestSagaData { CorrelationId = Guid.NewGuid() };

        await saga.ExecuteAsync(data, CancellationToken.None);
        
        // verify repo was called multiple times

        await _repository.Received().SaveAsync(
            Arg.Is<SagaState>(s => s.CurrentStep == "Step1"),
            Arg.Any<CancellationToken>());

        await _repository.Received().SaveAsync(
            Arg.Is<SagaState>(s => s.CurrentStep == "Step2" && s.Status == SagaStatus.Compensated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeFromStepAsync_ShouldResumeFromSpecificStep_WhenSagaIsWaiting()
    {
        var correlationId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();

        _step1.Order.Returns(1);
        _step1.StepName.Returns("Step1");

        _step2.Order.Returns(2);
        _step2.StepName.Returns("Step2");

        _step3.Order.Returns(3);
        _step3.StepName.Returns("Step3");
        _step3.ExecuteAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.SuccessResult());

        var sagaState = new SagaState
        {
            Id = sagaId,
            CorrelationId = correlationId,
            Status = SagaStatus.WaitingForEvent,
            SagaType = "TestSaga",
            CurrentStep = "Step2",
            Payload = JsonSerializer.Serialize(new TestSagaData { CorrelationId = correlationId }),
            Context = JsonSerializer.Serialize(new TestSagaContext()),
            Steps = new List<SagaStepLog>
            {
                new() { StepName = "Step1", Status = StepStatus.Completed },
                new() { StepName = "Step2", Status = StepStatus.Completed }
            }
        };

        _repository.GetByCorrelationIdAsync(correlationId, "TestSaga", Arg.Any<CancellationToken>())
            .Returns(sagaState);

        var saga = new TestSaga(_repository, new[] { _step1, _step2, _step3 }, _logger);
        var data = new TestSagaData { CorrelationId = correlationId };
        var context = new TestSagaContext();

        var result = await saga.ResumeFromStepAsync(data, context, "Step3", CancellationToken.None);

        Assert.True(result.IsSuccess);

        await _step1.DidNotReceive().ExecuteAsync(
            Arg.Any<TestSagaData>(),
            Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());

        await _step2.DidNotReceive().ExecuteAsync(
            Arg.Any<TestSagaData>(),
            Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());

        await _step3.Received(1).ExecuteAsync(
            Arg.Any<TestSagaData>(),
            Arg.Any<TestSagaContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeFromStepAsync_ShouldFail_WhenSagaNotFound()
    {
        var correlationId = Guid.NewGuid();

        _repository.GetByCorrelationIdAsync(correlationId, "TestSaga", Arg.Any<CancellationToken>())
            .Returns((SagaState?)null);

        var saga = new TestSaga(_repository, new[] { _step1, _step2 }, _logger);
        var data = new TestSagaData { CorrelationId = correlationId };
        var context = new TestSagaContext();

        var result = await saga.ResumeFromStepAsync(data, context, "Step2", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Saga state not found", result.ErrorMessage);
    }
    
    
    [Fact]
    public async Task ResumeFromStepAsync_ShouldReturnSuccess_WhenSagaAlreadyCompleted()
    {
        var correlationId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();

        var sagaState = new SagaState
        {
            Id = sagaId,
            CorrelationId = correlationId,
            Status = SagaStatus.Completed,
            SagaType = "TestSaga",
            Payload = JsonSerializer.Serialize(new TestSagaData()),
            Context = JsonSerializer.Serialize(new TestSagaData())
        };
        
        
    }

    [Fact]
    public async Task CompensateAsync_ShouldOnlyCompensateCompletedSteps_InReverseOrder()
    {
        var sagaId = Guid.NewGuid();

        var step3 = Substitute.For<ISagaStep<TestSagaData, TestSagaContext>>();
        
        _step1.StepName.Returns("Step1");
        _step2.Order.Returns(1);
        _step2.StepName.Returns("Step2");
        _step2.Order.Returns(2);
        step3.StepName.Returns("Step3");
        step3.Order.Returns(3);

        var sagaState = new SagaState
        {
            Id = sagaId,
            Status = SagaStatus.Running,
            Payload = JsonSerializer.Serialize(new TestSagaData()),
            Context = JsonSerializer.Serialize(new TestSagaContext()),
            Steps = new List<SagaStepLog>
            {
                new() { StepName = "Step1", Status = StepStatus.Completed },
                new() { StepName = "Step2", Status = StepStatus.Completed },
                new() { StepName = "Step3", Status = StepStatus.Failed }
                
            }
        };

        _repository.GetByIdAsync(sagaId, Arg.Any<CancellationToken>()).Returns(sagaState);
        
        var saga =  new TestSaga(_repository, [_step1, _step2], _logger);

        await saga.CompensateAsync(sagaId, CancellationToken.None);
        
        Received.InOrder( () =>
        {
             _step2.CompensateAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(),
                Arg.Any<CancellationToken>());
             _step1.CompensateAsync(Arg.Any<TestSagaData>(), Arg.Any<TestSagaContext>(),
                Arg.Any<CancellationToken>());
        });
        
        
    }
        
}































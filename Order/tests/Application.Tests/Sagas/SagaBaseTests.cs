using System.Text.Json;
using Application.Sagas;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Sagas;

public class SagaBaseTests
{
    private readonly ISagaRepository _repository = Substitute.For<ISagaRepository>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    
    // mock the steps
    private readonly ISagaStep<TestSagaData, TestSagaContext> _step1 =
        Substitute.For<ISagaStep<TestSagaData, TestSagaContext>>();

    private readonly ISagaStep<TestSagaData, TestSagaContext> _step2 =
        Substitute.For<ISagaStep<TestSagaData, TestSagaContext>>();

    private class TestSaga : SagaBase<TestSagaData, TestSagaContext>
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
    public async Task CompensateAsync_ShouldOnlyCompensateCompletedSteps_InReverseOrder()
    {
        var sagaId = Guid.NewGuid();

        _step1.StepName.Returns("Step1");
        _step2.Order.Returns(1);
        _step2.StepName.Returns("Step2");
        _step2.Order.Returns(2);

        var sagaState = new SagaState
        {
            Id = sagaId,
            Status = SagaStatus.Running,
            Payload = JsonSerializer.Serialize(new TestSagaData()),
            Context = JsonSerializer.Serialize(new TestSagaContext()),
            Steps = new List<SagaStepLog>
            {
                new() { StepName = "Step1", Status = StepStatus.Completed },
                new() { StepName = "Step2", Status = StepStatus.Completed }
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































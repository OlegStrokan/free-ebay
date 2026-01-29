using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga;

public sealed class ReturnSaga(
    ISagaRepository sagaRepository,
    IEnumerable<ISagaStep<ReturnSagaData, ReturnSagaContext>> steps,
    ILogger<ReturnSaga> logger)
    : SagaBase<ReturnSagaData, ReturnSagaContext>(sagaRepository, steps, logger), IReturnSaga
{
    protected override string SagaType => "ReturnSaga";
}

using Application.Interfaces;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga;

public sealed class OrderSaga(
    ISagaRepository sagaRepository,
    IEnumerable<ISagaStep<OrderSagaData, OrderSagaContext>> steps,
    ISagaErrorClassifier errorClassifier,
    ILogger<OrderSaga> logger)
    : SagaBase<OrderSagaData, OrderSagaContext>(sagaRepository, steps,errorClassifier, logger), IOrderSaga
{
    protected override string SagaType => "OrderSaga";
}
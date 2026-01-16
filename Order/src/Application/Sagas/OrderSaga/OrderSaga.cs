
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga;

public sealed class OrderSaga(
    ISagaRepository repository,
    IEnumerable<ISagaStep<OrderSagaData, OrderSagaContext>> steps,
    ILogger<OrderSaga> logger)
    : SagaBase<OrderSagaData, OrderSagaContext>(repository, steps, logger), IOrderSaga
{
    protected override string SagaType => "OrderSaga";
}
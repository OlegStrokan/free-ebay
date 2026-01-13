using Application.DTOs;
using Application.Sagas.Handlers;
using Application.Sagas.Persistence;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga;

public class OrderCreatedEventHandler
    : SagaEventHandler<OrderCreatedEventDto, OrderSagaData, OrderSagaContext>
{
    public override string EventType => "OrderCreatedEvent";
    public override string SagaType => "OrderSaga";

    public OrderCreatedEventHandler(
        IOrderSaga saga,
        ISagaRepository sagaRepository,
        ILogger<OrderCreatedEventHandler> logger)
        : base(saga, sagaRepository, logger)
    {}

    protected override OrderSagaData MapEventToSagaData(OrderCreatedEventDto eventDto)
    {
        return new OrderSagaData
        {
            CorrelationId = eventDto.OrderId,
            // OrderId = eventDto.OrderId,
            CustomerId = eventDto.CustomerId,
            Items = eventDto.Items,
            TotalAmount = eventDto.TotalAmount,
            Currency = eventDto.Currency,
            PaymentMethod = eventDto.PaymentMethod ?? "stripe",
            DeliveryAddress = eventDto.DeliveryAddress
        };
    }
}
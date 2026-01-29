using Application.DTOs;
using Application.Sagas.Handlers;
using Application.Sagas.Handlers.SagaCreation;
using Application.Sagas.Persistence;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.ReturnSaga;

public class OrderReturnRequestedEventHandler
: SagaEventHandler<OrderReturnRequestedEventDto, ReturnSagaData, ReturnSagaContext>
{
    public override string EventType => "OrderReturnRequestEvent";
    public override string SagaType => "ReturnSaga";

    public OrderReturnRequestedEventHandler(
        IReturnSaga saga,
        ISagaRepository sagaRepository,
        ILogger<OrderReturnRequestedEventHandler> logger)
        : base(saga, sagaRepository, logger)
    { }

    protected override ReturnSagaData MapEventToSagaData(OrderReturnRequestedEventDto eventDto)
    {
        return new ReturnSagaData
        {
            CorrelationId = eventDto.OrderId,
            CustomerId = eventDto.CustomerId,
            ReturnReason = eventDto.Reason,
            ReturnedItems = eventDto.ItemToReturn,
            RefundAmount = eventDto.RefundAmount,
            Currency = eventDto.Currency
        };
    }
}
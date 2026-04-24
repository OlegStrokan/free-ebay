using Application.DTOs;
using Application.Sagas.Handlers.SagaContinuation;
using Application.Sagas.Persistence;
using Microsoft.Extensions.Logging;

namespace Application.Sagas.OrderSaga;

public sealed class PaymentSucceededEventHandler
    : SagaContinuationEventHandler<PaymentSucceededEventDto, OrderSagaData, OrderSagaContext>
{
    public override string EventType => "PaymentSucceededEvent";
    public override string SagaType => "OrderSaga";

    protected override string ResumeAtStepName => "AwaitPaymentConfirmation";

    public PaymentSucceededEventHandler(
        IOrderSaga saga,
        ISagaRepository sagaRepository,
        ISagaDistributedLock distributedLock,
        ILogger<PaymentSucceededEventHandler> logger)
        : base(saga, sagaRepository, distributedLock, logger)
    {
    }

    protected override Guid ExtractCorrelationId(PaymentSucceededEventDto eventDto)
    {
        return Guid.TryParse(eventDto.OrderId, out var orderId)
            ? orderId
            : Guid.Empty;
    }

    protected override void UpdateContextFromEvent(PaymentSucceededEventDto eventDto, OrderSagaContext context)
    {
        if (!string.IsNullOrWhiteSpace(eventDto.PaymentId))
        {
            context.PaymentId = eventDto.PaymentId;
        }

        context.ProviderPaymentIntentId = eventDto.ProviderPaymentIntentId;
        context.PaymentStatus = OrderSagaPaymentStatus.Succeeded;
        context.PaymentFailureCode = null;
        context.PaymentFailureMessage = null;
    }
}

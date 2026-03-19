using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Gateways;

public interface IOrderCallbackPayloadSerializer
{
    string SerializePaymentSucceeded(string callbackEventId, Payment payment);

    string SerializePaymentFailed(string callbackEventId, Payment payment, FailureReason reason);

    string SerializeRefundSucceeded(string callbackEventId, Payment payment, Refund refund);

    string SerializeRefundFailed(string callbackEventId, Payment payment, Refund refund, FailureReason reason);
}
using Application.Gateways;
using Application.Gateways.Exceptions;
using Domain.ValueObjects;
using Grpc.Core;

namespace Infrastructure.Gateways;

public sealed class PaymentGateway(
    PaymentApi.PaymentApiClient client,
    ILogger<PaymentGateway> logger) : IPaymentGateway
{
    public async Task<string> ProcessPaymentAsync(
        Guid orderId,
        Guid customerId, 
        decimal amount,
        string currency, 
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        var request = new ProcessPaymentRequest(
            OrderId: orderId.ToString(),
            CustomerId: customerId.ToString(),
            Amount: (double)amount,
            Currency: currency,
            PaymentMethod: paymentMethod);

        try
        {
            var response = await client.ProcessPaymentAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                // @think: does another method exists to map error?
                throw response.ErrorCode switch
                {
                    "INSUFFICIENT_FUNDS" => new InsufficientFundsException(
                        $"Payment failed: insufficient funds. OrderId={orderId}, Amount={amount} {currency}"),
                    "PAYMENT_DECLINED" => new PaymentDeclinedException(
                        $"Payment declined by provider. OrderId={orderId}, Reason={response.ErrorMessage}"),
                    _ => new InvalidOperationException(
                        $"Payment processing failed. OrderId={orderId}, Error={response.ErrorMessage}")
                };
            }

            logger.LogInformation(
                "Payment processed successfully. OrderId={OrderId}, PaymentId={PaymentId}, Amount={Amount} {Currency}",
                orderId,
                response.PaymentId,
                amount,
                currency);

            return response.PaymentId;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new PaymentDeclinedException(
                $"Invalid payment data for OrderId={orderId}. Detail={ex.Status.Detail}");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InsufficientFundsException(
                $"Payment precondition failed for OrderId={orderId}. Detail={ex.Status.Detail}");
        }
    }

    public async Task<string> RefundAsync(
        string paymentId, 
        decimal amount, 
        string reason, 
        CancellationToken cancellationToken)
    {
        var request = new RefundPaymentRequest(
            PaymentId: paymentId,
            Amount: (double)amount,
            Reason: reason
        );

        try
        {
            var response = await client.RefundPaymentAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                throw new InvalidOperationException(
                    $"Refund failed. PaymentId={paymentId}, Amount={amount}, Error={response.ErrorMessage}");
            }

            logger.LogInformation(
                "Refund processed successfully. PaymentId={PaymentId}, RefundId={RefundId}, Amount={Amount}",
                paymentId,
                response.RefundId,
                amount);

            return response.RefundId;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Payment not found for refund. PaymentId={paymentId}. Detail={ex.Status.Detail}");
        }
    }


    private sealed record ProcessPaymentRequest(
        string OrderId,
        string CustomerId,
        double Amount,
        string Currency,
        string PaymentMethod);

    private sealed record RefundPaymentRequest(
        string PaymentId,
        double Amount,
        string Reason);
}
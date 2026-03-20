using Application.Gateways;
using Application.Gateways.Exceptions;
using Domain.ValueObjects;
using Grpc.Core;
using Infrastructure.Extensions;
using Protos.Payment;
using StatusCode = Grpc.Core.StatusCode;

namespace Infrastructure.Gateways;

public sealed class PaymentGateway(
    PaymentService.PaymentServiceClient  client,
    ILogger<PaymentGateway> logger) : IPaymentGateway
{
    public async Task<PaymentProcessingResult> ProcessPaymentAsync(
        Guid orderId,
        Guid customerId, 
        decimal amount,
        string currency, 
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        var request = new ProcessPaymentRequest
        {
            OrderId = orderId.ToString(),
            CustomerId = customerId.ToString(),
            Amount = amount.ToDecimalValue(),
            Currency = currency,
            PaymentMethod = paymentMethod
        };

        try
        {
            var response = await client.ProcessPaymentAsync(request, cancellationToken: cancellationToken);
            var status = MapPaymentStatus(response);

            var providerPaymentIntentId = string.IsNullOrWhiteSpace(response.ProviderPaymentIntentId)
                ? null
                : response.ProviderPaymentIntentId;

            var clientSecret = string.IsNullOrWhiteSpace(response.ClientSecret)
                ? null
                : response.ClientSecret;

            var errorCode = string.IsNullOrWhiteSpace(response.ErrorCode)
                ? null
                : response.ErrorCode;

            var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? null
                : response.ErrorMessage;

            if (status == PaymentProcessingStatus.Failed)
            {
                // it's more clear than extracting it to a separate method
                // eventually we will move it when we have like 20+ error codes
                throw errorCode switch
                {
                    "INSUFFICIENT_FUNDS" => new InsufficientFundsException(
                        $"Payment failed: insufficient funds. OrderId={orderId}, Amount={amount} {currency}"),
                    "PAYMENT_DECLINED" => new PaymentDeclinedException(
                        $"Payment declined by provider. OrderId={orderId}, Reason={errorMessage}"),
                    _ => new InvalidOperationException(
                        $"Payment processing failed. OrderId={orderId}, Error={errorMessage}")
                };
            }

            if (string.IsNullOrWhiteSpace(response.PaymentId))
            {
                throw new InvalidOperationException(
                    $"Payment service returned empty PaymentId for non-failed status. OrderId={orderId}");
            }

            logger.LogInformation(
                "Payment processed. OrderId={OrderId}, PaymentId={PaymentId}, Status={Status}, Amount={Amount} {Currency}",
                orderId,
                response.PaymentId,
                status,
                amount,
                currency);

            return new PaymentProcessingResult(
                PaymentId: response.PaymentId,
                Status: status,
                ProviderPaymentIntentId: providerPaymentIntentId,
                ClientSecret: clientSecret,
                ErrorCode: errorCode,
                ErrorMessage: errorMessage);
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
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new GatewayUnavailableException(
                GatewayUnavailableReason.Timeout,
                $"Payment service deadline exceeded for OrderId={orderId}. gRPC={ex.StatusCode}: {ex.Status.Detail}",
                ex);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            throw new GatewayUnavailableException(
                GatewayUnavailableReason.ServiceUnavailable,
                $"Payment service unavailable for OrderId={orderId}. gRPC={ex.StatusCode}: {ex.Status.Detail}",
                ex);
        }
    }

    private static PaymentProcessingStatus MapPaymentStatus(ProcessPaymentResponse response)
    {
        var rawStatus = (int)response.Status;

        return rawStatus switch
        {
            1 => PaymentProcessingStatus.Succeeded,
            2 => PaymentProcessingStatus.Pending,
            3 => PaymentProcessingStatus.Failed,
            4 => PaymentProcessingStatus.RequiresAction,
            _ => response.Success
                ? PaymentProcessingStatus.Succeeded
                : PaymentProcessingStatus.Failed,
        };
    }

    public async Task<string> RefundAsync(
        string paymentId, 
        decimal amount, 
        string reason, 
        CancellationToken cancellationToken)
    {
        var request = new RefundPaymentRequest
        {
            PaymentId = paymentId,
            Amount = amount.ToDecimalValue(),
            Reason = reason
        };

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


}
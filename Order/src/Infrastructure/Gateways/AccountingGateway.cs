using Application.DTOs;
using Grpc.Core;

namespace Infrastructure.Gateways;

public class AccountingGateway(
    AccountingService.AccountingServiceClient client,
    ILogger<AccountingGateway> logger) 
{
    public async Task<string> RecordRefundAsync(
        Guid orderId,
        string refundId,
        decimal amount,
        string currency,
        string reason,
        CancellationToken cancellationToken)
    {

        var request = new RecordRefundRequest(
            OrderId: orderId.ToString(),
            RefundId: refundId,
            Amount: (double)amount,
            Currency: currency,
            Reason: reason);

        try
        {
            var response = await client.RecordRefundAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
                throw new InvalidOperationException(
                    $"Recording refund failed. OrderId={orderId}, RefundId={refundId}, Error={response.ErrorMessage}");

            logger.LogInformation(
                "Refund recorded in accounting. OrderId={OrderId}, RefundId={RefundId}, TransactionId={TransactionId}",
                orderId,
                refundId,
                response.TransactionId);

            return response.TransactionId;
        } 
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new InvalidOperationException(
                $"Invalid refund data for OrderId={orderId}. Detail={ex.Status.Detail}");
        }
        
    }

    public async Task<string> ReserveRevenueAsync(
        Guid orderId,
        decimal amount,
        string currency,
        List<OrderItemDto> returnedItems,
        CancellationToken cancellationToken)
    {
        var request = new ReverseRevenueRequest(
            OrderId: orderId.ToString(),
            Amount: (double)amount,
            Currency: currency);

        request.ReturnedItems.AddRange(returnedItems.Select(i => new AccountingItem(
            ProductId: i.ProductId.ToString(),
            Quantity: i.Quantity,
            Price: (double)i.Price,
            Currency: i.Currency
        )));

        try
        {
            var response = await client.ReverseRevenueAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
                throw new InvalidOperationException(
                    $"Revenue reversal failed. OrderId={orderId}, Amount={amount} {currency}, Error={response.ErroMessage}");

            logger.LogInformation(
                "Revenue reversed in accounting. OrderId={OrderId}, ReversalId={ReversalId}, Amount={Amount} {Currency}",
                orderId,
                response.ReversalId,
                amount,
                currency);

            return response.ReversalId;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Order not found in accounting system. OrderId={orderId}. Detail={ex.Status.Detail}");
        }
    }

    public async Task CancelRevenueReversalAsync(
        string reversalId,
        string reason,
        CancellationToken cancellationToken)
    {
        var request = new CancelReversalRequest(
            ReversalId: reversalId,
            Reason: reason
        );

        try
        {
            var response = await client.CancelReversalRequest(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                throw new InvalidOperationException(
                    $"Canceling revenue reversal failed. ReversalId={reversalId}, Error={response.ErrorMessage}");
            }

            logger.LogInformation(
                "Revenue reversal cancelling in accounting. ReversalId={ReversalId}",
                reversalId);
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.NotFound)
        {
            // idempotent: already cancelled/not found
            logger.LogWarning(
                "CancelRevenueReversal: reversal not found (treated as idempontent success). ReversalId={ReversalId}",
                reversalId);
        }
    }
    


    private sealed record RecordRefundRequest(
        string OrderId,
        string RefundId,
        double Amount,
        string Currency,
        string Reason);

    private sealed record ReverseRevenueRequest(
        string OrderId,
        double Amount,
        string Currency);

    private sealed record AccountingItem(
        string ProductId,
        int Quantity,
        double Price,
        string Currency);

    private sealed record CancelReversalRequest(
        string ReversalId,
        string Reason);
}
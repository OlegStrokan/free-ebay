using Application.Common.Attributes;

namespace Application.Gateways;

public interface IPaymentGateway
{
    [Retry(maxRetries: 5, delayMilliseconds: 1000, exponentialBackoff: true)]
    Task<string> ProcessPaymentAsync(
        Guid orderId, 
        Guid customerId,
        decimal amount,  
        string currency,
        string paymentMethod,
        CancellationToken cancellationToken
        );
    
    [Retry(maxRetries:3, delayMilliseconds: 500)]
    Task<string> RefundAsync(
        string paymentId, 
        decimal amount,
        string reason,
        CancellationToken cancellationToken
        );
}
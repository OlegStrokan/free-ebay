
namespace Application.Gateways;

public interface IPaymentGateway
{
    Task<PaymentProcessingResult> ProcessPaymentAsync(
        Guid orderId, 
        Guid customerId,
        decimal amount,  
        string currency,
        string paymentMethod,
        CancellationToken cancellationToken
        );
    
    Task<string> RefundAsync(
        string paymentId, 
        decimal amount,
        string currency,
        string reason,
        CancellationToken cancellationToken
        );
}

public enum PaymentProcessingStatus
{
    Succeeded = 0,
    Pending = 1,
    RequiresAction = 2,
    Failed = 3,
}

public sealed record PaymentProcessingResult(
    string? PaymentId,
    PaymentProcessingStatus Status,
    string? ProviderPaymentIntentId = null,
    string? ClientSecret = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);
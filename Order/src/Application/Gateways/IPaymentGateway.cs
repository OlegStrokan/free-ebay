namespace Application.Gateways;

public interface IPaymentGateway
{
    Task<string> ProcessPaymentAsync(
        Guid orderId, 
        Guid customerId,
        decimal amount,  
        string currency,
        string paymentMethod,
        CancellationToken cancellationToken
        );
    Task RefundAsync(
        string paymentId, 
        decimal amount,
        string reason,
        CancellationToken cancellationToken
        );
}
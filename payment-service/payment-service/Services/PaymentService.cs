using payment_service.Entities;
using payment_service.Enums;
using Stripe;

namespace payment_service.Services;

public class MyPaymentService
{
    private readonly ILogger<MyPaymentService> _logger;

    public MyPaymentService(ILogger<MyPaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentEntity> ProcessPayment(PaymentEntity payment)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = payment.Amount.ToStripeAmount(),  
                Currency = payment.Amount.Currency.ToLower(),
                PaymentMethodTypes = new List<string> { "card" } // For now, support card only
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            payment.ClientSecret = intent.ClientSecret;
            payment.PaymentStatus = PaymentStatus.Pending;

            _logger.LogInformation("Created PaymentIntent with ID: {Id}", intent.Id);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for Payment: {@Payment}", payment);
            payment.PaymentStatus = PaymentStatus.Failed;
            return payment;
        }
    }
}
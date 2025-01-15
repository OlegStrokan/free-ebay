

using payment_service.Entities;

namespace payment_service.Services;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly string _topic = "payment";

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<Payment> ProcessPayment(Payment payment)
    {
        try
        {
            // Save Payment to the Database
            // using (var dbContext = new PaymentDbContext())
            // {
            //     await dbContext.Payments.AddAsync(payment);
            //     await dbContext.SaveChangesAsync();
            // }

            return payment;
        }
        catch (Exception ex)
        {
            throw new Exception("Error processing payment", ex);
        }
    }
}


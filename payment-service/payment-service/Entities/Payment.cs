using payment_service.Enums;

namespace payment_service.Entities;

public class Payment
{
    public string OrderId { get; set; }
    public Money Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    
}
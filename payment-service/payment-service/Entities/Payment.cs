using payment_service.Enums;

namespace payment_service.Entities;

public class PaymentEntity
{
    public string Id { get; set; } 
    public string OrderId { get; set; }
    public MoneyEntity Amount { get; set; } = new MoneyEntity();
    public string PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string ClientSecret { get; set; } 
}
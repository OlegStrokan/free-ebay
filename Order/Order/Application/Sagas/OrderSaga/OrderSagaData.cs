using Application.Common.Enums;
using Application.DTOs;

namespace Application.Sagas.OrderSaga;

public sealed class OrderSagaData : SagaData
{
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod PaymentMethod { get; set; }
    public AddressDto DeliveryAddress { get; set; } = null!;
    public string? PaymentIntentId { get; set; }
    public ShippingCarrier ShippingCarrier { get; set; } = ShippingCarrier.Dpd;
}
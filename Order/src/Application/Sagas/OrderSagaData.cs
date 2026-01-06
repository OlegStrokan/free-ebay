using Application.DTOs;

namespace Application.Sagas;

public sealed class OrderSagaData
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty;
    public AddressDto DeliveryAddress { get; set; } = null!;
}
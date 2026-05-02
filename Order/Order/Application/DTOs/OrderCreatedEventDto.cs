using Application.Common.Enums;

namespace Application.DTOs;

public sealed record OrderCreatedEventDto
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public AddressDto DeliveryAddress { get; set; } = null!;
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentIntentId { get; set; }
    public DateTime CreatedAt { get; set; }
}
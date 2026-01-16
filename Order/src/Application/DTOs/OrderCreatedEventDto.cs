namespace Application.DTOs;

public sealed record OrderCreatedEventDto
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public AddressDto DeliveryAddress { get; set; } = null!;
    public List<OrderItemDto> Items { get; set; } = new();
    // todo: add enum
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
}
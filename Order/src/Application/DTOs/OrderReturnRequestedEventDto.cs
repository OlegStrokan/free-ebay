namespace Application.DTOs;

public record OrderReturnRequestedEventDto
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<OrderItemDto> ItemToReturn { get; init; } = new();
    public decimal RefundAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
}

public record ReturnRequestDto(
    Guid OrderId,
    string Reason,
    List<OrderItemDto> ItemsToReturn
    );
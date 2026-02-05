namespace Domain.Exceptions;

public class OrderNotFoundException(Guid orderId)
    : DomainException($"Order with ID {orderId} was not found.")
{
    public Guid OrderId { get; } = orderId;
}
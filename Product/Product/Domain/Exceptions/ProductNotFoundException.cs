namespace Domain.Exceptions;

public class ProductNotFoundException(Guid productId)
    : DomainException($"Product with ID {productId} was not found.")
{
    public Guid ProductId { get; } = productId;
}

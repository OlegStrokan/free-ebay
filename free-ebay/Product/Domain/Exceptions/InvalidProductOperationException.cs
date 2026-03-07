namespace Domain.Exceptions;

public class InvalidProductOperationException(Guid productId, string reason)
    : DomainException($"Invalid operation on product {productId}: {reason}")
{
    public Guid   ProductId { get; } = productId;
    public string Reason    { get; } = reason;
}

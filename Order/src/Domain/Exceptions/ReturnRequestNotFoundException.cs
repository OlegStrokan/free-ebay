namespace Domain.Exceptions;

public class ReturnRequestNotFoundException(Guid returnRequestId)
    : DomainException($"ReturnRequest for order {returnRequestId} not found")
{
    public Guid ReturnRequestId { get; } = returnRequestId;

}
    

namespace Domain.Exceptions;

public class ReturnRequestNotFoundException(Guid orderId)
    : DomainException($"ReturnRequest for order {orderId} not found");
    // @think: should i have OrderId as private field? check OrderNotFoundException
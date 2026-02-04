namespace Domain.Exceptions;

// @think: should we separate exception per entity? orderDomainException, returnRequestDomainException?
public class DomainException : Exception
{
    public DomainException(string message) : base(message) {}
    
    public DomainException(string message, Exception innerException)
        : base (message, innerException) {}
}
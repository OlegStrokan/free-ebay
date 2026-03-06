namespace Domain.Exceptions;

public class InvalidStateException(string message) : DomainException(message);
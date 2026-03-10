namespace Domain.Exceptions;

public class InvalidValueException(string message) : DomainException(message);

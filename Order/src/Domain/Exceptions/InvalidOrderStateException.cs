namespace Domain.Exceptions;

public class InvalidOrderStateException(string message) : OrderDomainException(message);
namespace Application.Gateways.Exceptions;

public class InsufficientFundsException(string message) : Exception(message)
{
}
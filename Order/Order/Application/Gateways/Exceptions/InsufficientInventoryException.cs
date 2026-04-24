namespace Application.Gateways.Exceptions;

public class InsufficientInventoryException(string message) : Exception(message)
{
}
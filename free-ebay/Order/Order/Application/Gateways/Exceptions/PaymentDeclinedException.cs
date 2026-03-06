namespace Application.Gateways.Exceptions;

public class PaymentDeclinedException(string message) : Exception(message)
{
}
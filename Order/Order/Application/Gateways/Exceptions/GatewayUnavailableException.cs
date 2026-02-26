namespace Application.Gateways.Exceptions;

public class GatewayUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

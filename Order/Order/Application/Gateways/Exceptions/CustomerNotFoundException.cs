namespace Application.Gateways.Exceptions;

public sealed class CustomerNotFoundException(Guid customerId, Exception? inner = null)
    : Exception($"Customer '{customerId}' was not found in User service.", inner);

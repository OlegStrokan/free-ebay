namespace Application.Gateways.Exceptions;

public sealed class ProductNotFoundException(IEnumerable<string> missingIds)
    : Exception($"Products not found: {string.Join(", ", missingIds)}");

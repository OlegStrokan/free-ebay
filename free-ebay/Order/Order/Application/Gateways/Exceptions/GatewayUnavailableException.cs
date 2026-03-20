namespace Application.Gateways.Exceptions;

public enum GatewayUnavailableReason
{
    Timeout = 0,
    ServiceUnavailable = 1,
}
// @think: we have 2 constructors, because payment use reason field, and others gateway not.
// should we use "reason" arg in all gateways = shall we differenciate between timeout and unavailable in all services?
public sealed class GatewayUnavailableException : Exception
{
    public GatewayUnavailableReason Reason { get; }

    public GatewayUnavailableException(string message, Exception? inner = null)
        : this(GatewayUnavailableReason.ServiceUnavailable, message, inner)
    {
    }

    public GatewayUnavailableException(
        GatewayUnavailableReason reason,
        string message,
        Exception? inner = null)
        : base(message, inner)
    {
        Reason = reason;
    }
}

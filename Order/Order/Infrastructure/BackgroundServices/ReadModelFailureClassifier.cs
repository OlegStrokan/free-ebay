using System.Net.Sockets;
using Npgsql;

namespace Infrastructure.BackgroundServices;

public enum ReadModelFailureKind
{
    /// Infrastructure is unavailable (DB down, network issue, timeout).
    /// Correct response: pause the partition and wait for recovery.
    Systemic,
    
    /// The message itself is problematic (bad payload, missing aggregate, domain logic error).
    /// Correct response: move to the durable retry store and commit the Kafka offset.
    MessageSpecific,
}

public static class ReadModelFailureClassifier
{
    public static ReadModelFailureKind Classify(Exception ex)
    {
        // Walk the inner exception chain - a message-specific exception can wrap a DB error
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (IsSystemic(current))
                return ReadModelFailureKind.Systemic;
        }

        return ReadModelFailureKind.MessageSpecific;
    }

    private static bool IsSystemic(Exception ex) =>
        ex is NpgsqlException
            or SocketException          
            or TimeoutException         
            or TaskCanceledException { InnerException: TimeoutException };   
}

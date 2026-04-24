using System.Net.Sockets;
using Infrastructure.Elasticsearch;

namespace Infrastructure.Kafka;

public enum FailureKind
{
    Systemic,
    MessageSpecific,
}

public static class FailureClassifier
{
    public static FailureKind Classify(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (IsSystemic(current))
                return FailureKind.Systemic;
        }

        if (ex is ElasticsearchIndexingException)
            return FailureKind.MessageSpecific;

        return FailureKind.Systemic;
    }

    private static bool IsSystemic(Exception ex)
    {
        return ex is HttpRequestException
            or SocketException
            or TaskCanceledException { InnerException: TimeoutException }
            or TimeoutException
            || IsElasticsearchServiceError(ex);
    }

    private static bool IsElasticsearchServiceError(Exception ex)
    {
        if (ex is not ElasticsearchIndexingException esEx)
            return false;

        var msg = esEx.Message;

        return msg.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unreachable", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("503", StringComparison.Ordinal)
            || msg.Contains("502", StringComparison.Ordinal)
            || msg.Contains("429", StringComparison.Ordinal)
            || msg.Contains("cluster", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unavailable", StringComparison.OrdinalIgnoreCase);
    }
}

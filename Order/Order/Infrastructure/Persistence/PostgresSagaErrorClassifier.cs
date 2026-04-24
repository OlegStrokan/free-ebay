using Application.Interfaces;
using Npgsql;

namespace Infrastructure.Persistence;

public class PostgresSagaErrorClassifier : ISagaErrorClassifier
{
    public bool IsTransient(Exception ex)
    {
        // check for postre transient errors (deadlocks, connection blips)
        if (ex is NpgsqlException npgsqlException && npgsqlException.IsTransient) return true;

        if (ex.InnerException is PostgresException pgException && pgException.SqlState == "40P01") return true;

        return ex is TimeoutException || ex is HttpRequestException;
    }
}
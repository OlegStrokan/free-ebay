using Application.RetryStore;
using Infrastructure.RetryStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.Infrastructure;

public abstract class PostgresFixture
{
    private PostgreSqlContainer _postgres = null!;
    protected string ConnectionString { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task FixtureSetUpAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();

        ConnectionString = _postgres.GetConnectionString();

        var opts = Options.Create(new RetryStoreOptions { ConnectionString = ConnectionString });
        var initializer = new RetryStoreInitializer(opts, NullLogger<RetryStoreInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);
    }

    [OneTimeTearDown]
    public async Task FixtureTearDownAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected PostgresRetryStore CreateStore()
    {
        var opts = Options.Create(new RetryStoreOptions { ConnectionString = ConnectionString });
        return new PostgresRetryStore(opts, NullLogger<PostgresRetryStore>.Instance);
    }

    protected RetryRecord BuildRecord(
        RetryRecordStatus status = RetryRecordStatus.Pending,
        int retryCount = 0,
        DateTime? nextRetryAt = null)
    {
        var now = DateTime.UtcNow;
        return new RetryRecord
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventType = "ProductCreatedEvent",
            Topic = "product.events",
            Partition = 0,
            Offset = Random.Shared.NextInt64(0, 100_000),
            MessageKey = "key-" + Guid.NewGuid().ToString("N")[..8],
            Payload = """{"EventId":"abc","EventType":"ProductCreatedEvent","Payload":{},"OccurredOn":"2026-01-01T00:00:00Z"}""",
            Headers = """{"event-type":"ProductCreatedEvent"}""",
            FirstFailureTime = now.AddMinutes(-10),
            LastFailureTime = now.AddMinutes(-5),
            RetryCount = retryCount,
            NextRetryAt = nextRetryAt ?? now.AddMinutes(-1),
            Status = status,
            LastErrorMessage = "test error",
            LastErrorType = "System.Exception",
            CorrelationId = "00-traceid-spanid-01",
        };
    }

    protected async Task<RetryRecord?> ReadRecordDirectAsync(Guid id)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            """SELECT status, retry_count, next_retry_at, last_error_message, last_error_type FROM retry_records WHERE id = @id""",
            conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new RetryRecord
        {
            Id = id,
            Status = Enum.Parse<RetryRecordStatus>(reader.GetString(0)),
            RetryCount = reader.GetInt32(1),
            NextRetryAt = reader.GetDateTime(2),
            LastErrorMessage = reader.IsDBNull(3) ? null : reader.GetString(3),
            LastErrorType = reader.IsDBNull(4) ? null : reader.GetString(4),
        };
    }
}

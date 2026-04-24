using FluentAssertions;
using Infrastructure.RetryStore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.RetryStore;

[TestFixture]
public sealed class RetryStoreInitializerTests
{
    private PostgreSqlContainer _postgres = null!;
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task FixtureSetUpAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task FixtureTearDownAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Test, Order(1)]
    public async Task StartAsync_ShouldCreateRetryRecordsTable()
    {
        var initializer = CreateInitializer();

        await initializer.StartAsync(CancellationToken.None);

        var exists = await TableExistsAsync("retry_records");
        exists.Should().BeTrue("RetryStoreInitializer must create the retry_records table");
    }

    [Test, Order(2)]
    public async Task StartAsync_CalledTwice_ShouldNotThrow()
    {
        var initializer = CreateInitializer();

        // First call creates the table, second should be idempotent (CREATE IF NOT EXISTS)
        await initializer.StartAsync(CancellationToken.None);

        Assert.DoesNotThrowAsync(() => initializer.StartAsync(CancellationToken.None));
    }

    [Test, Order(3)]
    public async Task StartAsync_ShouldCreatePartialIndex()
    {
        var initializer = CreateInitializer();
        await initializer.StartAsync(CancellationToken.None);

        var exists = await IndexExistsAsync("idx_retry_records_pending_due");
        exists.Should().BeTrue("RetryStoreInitializer must create the pending-due partial index");
    }

    [Test]
    public Task StopAsync_ShouldCompleteImmediately()
    {
        var initializer = CreateInitializer();

        Assert.DoesNotThrowAsync(() => initializer.StopAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    private RetryStoreInitializer CreateInitializer()
    {
        var opts = Options.Create(new RetryStoreOptions { ConnectionString = _connectionString });
        return new RetryStoreInitializer(opts, NullLogger<RetryStoreInitializer>.Instance);
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = @name)",
            conn);
        cmd.Parameters.AddWithValue("name", tableName);

        var result = await cmd.ExecuteScalarAsync();
        return result is true;
    }

    private async Task<bool> IndexExistsAsync(string indexName)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM pg_indexes WHERE indexname = @name)",
            conn);
        cmd.Parameters.AddWithValue("name", indexName);

        var result = await cmd.ExecuteScalarAsync();
        return result is true;
    }
}

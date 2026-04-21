using Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace Email.IntegrationTests.Services;

public sealed class PostgresProcessedMessageStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("email_integration")
        .WithUsername("test")
        .WithPassword("test")
        .WithImage("postgres:16-alpine")
        .Build();

    private PostgresProcessedMessageStore _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _sut = BuildStore(_postgres.GetConnectionString());
        await _sut.InitializeAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private static PostgresProcessedMessageStore BuildStore(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString
            })
            .Build();
        return new PostgresProcessedMessageStore(config, NullLogger<PostgresProcessedMessageStore>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        // calling again should not throw (CREATE TABLE IF NOT EXISTS)
        await _sut.InitializeAsync(CancellationToken.None);
    }

    [Fact]
    public async Task IsProcessedAsync_ReturnsFalse_ForNewMessage()
    {
        var messageId = Guid.NewGuid();

        var result = await _sut.IsProcessedAsync(messageId, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task MarkProcessedAsync_ThenIsProcessedAsync_ReturnsTrue()
    {
        var messageId = Guid.NewGuid();

        await _sut.MarkProcessedAsync(messageId, CancellationToken.None);
        var result = await _sut.IsProcessedAsync(messageId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task MarkProcessedAsync_IsIdempotent()
    {
        var messageId = Guid.NewGuid();

        await _sut.MarkProcessedAsync(messageId, CancellationToken.None);
        // second call should not throw (ON CONFLICT DO NOTHING)
        await _sut.MarkProcessedAsync(messageId, CancellationToken.None);

        Assert.True(await _sut.IsProcessedAsync(messageId, CancellationToken.None));
    }

    [Fact]
    public async Task IsProcessedAsync_DoesNotCrossContaminate_BetweenMessages()
    {
        var processed = Guid.NewGuid();
        var unprocessed = Guid.NewGuid();

        await _sut.MarkProcessedAsync(processed, CancellationToken.None);

        Assert.True(await _sut.IsProcessedAsync(processed, CancellationToken.None));
        Assert.False(await _sut.IsProcessedAsync(unprocessed, CancellationToken.None));
    }
}

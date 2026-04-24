using Email.IntegrationTests.Infrastructure;
using Email.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Email.IntegrationTests.Services;

[Collection("Integration")]
public sealed class PostgresProcessedMessageStoreTests(IntegrationFixture fixture)
{
    private IProcessedMessageStore Store =>
        fixture.Services.GetRequiredService<IProcessedMessageStore>();

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        await Store.InitializeAsync(CancellationToken.None);
    }

    [Fact]
    public async Task IsProcessedAsync_ReturnsFalse_ForNewMessage()
    {
        var result = await Store.IsProcessedAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkProcessedAsync_ThenIsProcessedAsync_ReturnsTrue()
    {
        var messageId = Guid.NewGuid();

        await Store.MarkProcessedAsync(messageId, CancellationToken.None);

        (await Store.IsProcessedAsync(messageId, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task MarkProcessedAsync_IsIdempotent()
    {
        var messageId = Guid.NewGuid();

        await Store.MarkProcessedAsync(messageId, CancellationToken.None);
        // second call must not throw (ON CONFLICT DO NOTHING)
        await Store.MarkProcessedAsync(messageId, CancellationToken.None);

        (await Store.IsProcessedAsync(messageId, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task IsProcessedAsync_DoesNotCrossContaminate_BetweenMessages()
    {
        var processed = Guid.NewGuid();
        var unprocessed = Guid.NewGuid();

        await Store.MarkProcessedAsync(processed, CancellationToken.None);

        (await Store.IsProcessedAsync(processed, CancellationToken.None)).Should().BeTrue();
        (await Store.IsProcessedAsync(unprocessed, CancellationToken.None)).Should().BeFalse();
    }
}

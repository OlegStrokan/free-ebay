using System.Collections.Concurrent;
using Infrastructure.Messaging;
using Infrastructure.Persistence.Entities;

namespace Inventory.IntegrationTests.TestHelpers;

public sealed class FakeOutboxPublisher : IOutboxPublisher
{
    private readonly ConcurrentQueue<Guid> publishedMessageIds = new();

    public bool ShouldFail { get; set; }

    public IReadOnlyCollection<Guid> PublishedMessageIds => publishedMessageIds.ToArray();

    public Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken)
    {
        if (ShouldFail)
            throw new InvalidOperationException("Simulated publish failure");

        publishedMessageIds.Enqueue(message.OutboxMessageId);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        ShouldFail = false;

        while (publishedMessageIds.TryDequeue(out _))
        {
        }
    }
}

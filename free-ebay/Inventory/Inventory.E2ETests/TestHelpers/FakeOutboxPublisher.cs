using Infrastructure.Messaging;
using Infrastructure.Persistence.Entities;

namespace Inventory.E2ETests.TestHelpers;

public sealed class FakeOutboxPublisher : IOutboxPublisher
{
    public List<Guid> PublishedMessageIds { get; } = [];

    public Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken)
    {
        PublishedMessageIds.Add(message.OutboxMessageId);
        return Task.CompletedTask;
    }

    public void Reset() => PublishedMessageIds.Clear();
}

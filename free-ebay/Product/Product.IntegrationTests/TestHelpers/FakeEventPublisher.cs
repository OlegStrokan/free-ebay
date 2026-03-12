using Application.Interfaces;
using Domain.Common;

namespace Product.IntegrationTests.TestHelpers;

public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly object _lock = new();
    private readonly List<(Guid Id, string TypeName, string Content, string AggregateId)> _published = new();

    public IReadOnlyList<(Guid Id, string TypeName, string Content, string AggregateId)> Published
    {
        get { lock (_lock) { return _published.ToList(); } }
    }

    // When true, PublishRawAsync throws to simulate a broker outage
    public bool ShouldFail { get; set; }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
        => Task.CompletedTask;

    public Task PublishRawAsync(
        Guid id, string typeName, string content, DateTime occurredOn, string aggregateId,
        CancellationToken cancellationToken)
    {
        if (ShouldFail)
            throw new InvalidOperationException("FakeEventPublisher: simulated broker failure.");

        lock (_lock)
        {
            _published.Add((id, typeName, content, aggregateId));
        }

        return Task.CompletedTask;
    }
}

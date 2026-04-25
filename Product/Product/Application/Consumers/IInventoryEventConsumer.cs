using System.Text.Json;

namespace Application.Consumers;

public interface IInventoryEventConsumer
{
    string EventType { get; }
    Task ConsumeAsync(JsonElement payload, CancellationToken ct);
}

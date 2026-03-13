using System.Text.Json;

namespace Application.Consumers;

public interface IProductEventConsumer
{
    string EventType { get; }
    Task ConsumeAsync(JsonElement payload, CancellationToken ct);
}

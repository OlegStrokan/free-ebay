using System.Text.Json;

namespace Infrastructure.Kafka;

public sealed class EventWrapper
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
    public DateTime OccurredOn { get; set; }
}

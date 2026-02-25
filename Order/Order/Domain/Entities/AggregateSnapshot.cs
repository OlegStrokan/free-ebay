namespace Domain.Entities;

public class AggregateSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AggregateId { get; set; } = null!;
    public string AggregateType { get; set; } = null!;
    public int Version { get; set; }
    public string StateJson { get; set; } = null!;
    public DateTime TakenAn { get; set; }


    private AggregateSnapshot() {}

    public static AggregateSnapshot Create(
        string aggregateId, string aggregateType, int version, string stateJson)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
            throw new ArgumentException("AggregateId is required", nameof(aggregateId));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("AggregateType is required", nameof(aggregateType));

        if (version < 0)
            throw new ArgumentException("Version must be >= 0", nameof(version));

        if (string.IsNullOrWhiteSpace(stateJson))
            throw new ArgumentException("StateJson is required", nameof(stateJson));

        return new AggregateSnapshot
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            Version = version,
            StateJson = stateJson,
            TakenAn = DateTime.UtcNow
        };
    }
}
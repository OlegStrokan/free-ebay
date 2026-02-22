namespace Domain.Entities;

public class AggregateSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AggregateId { get; set; } = null!;
    public string AggregateTYpe { get; set; } = null!;
    public int Version { get; set; }
    public string StateJson { get; set; } = null!;
    public DateTime TakeAn { get; set; }
}
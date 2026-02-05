namespace Domain.Entities;

public class IdempotencyRecord
{
    public string Key { get; private set; } = null!;
    public Guid ResultId { get; private set; } 
    public DateTime CreatedAt { get; private set; }
    
    private IdempotencyRecord() {}

    public static IdempotencyRecord Create(
        string key,
        Guid resultId,
        DateTime createdAt)
    {
        return new IdempotencyRecord
        {
            Key = key,
            ResultId = resultId,
            CreatedAt = createdAt
        };
    }
}
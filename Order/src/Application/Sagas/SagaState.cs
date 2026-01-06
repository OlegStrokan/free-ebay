namespace Application.Sagas;

public sealed class SagaState
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public SagaStatus Status { get; set; }
    public SagaContext Context { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SagaStepLog> Steps { get; set; } = new();
}

public sealed class SagaStepLog
{
    public Guid Id { get; set; }
    public Guid SagaId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public string? Request { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    
}


public enum StepStatus
{
    Running,
    Completed,
    Failed
}
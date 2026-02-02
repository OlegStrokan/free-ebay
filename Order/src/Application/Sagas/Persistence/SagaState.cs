namespace Application.Sagas.Persistence;


// for db
public sealed class SagaState<TContext> where TContext : SagaContext
{
    public Guid Id { get; set; }
    
    public Guid CorrelationId { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public SagaStatus Status { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public TContext Context { get; set; } = default!;
    public string Payload { get; set; } = string.Empty;
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
    Failed,
    Compensated
}
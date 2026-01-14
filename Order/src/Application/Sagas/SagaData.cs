namespace Application.Sagas;


public abstract class SagaData
{
    public Guid CorrelationId { get; set; }
}
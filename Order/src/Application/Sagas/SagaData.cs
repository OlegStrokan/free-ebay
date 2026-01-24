namespace Application.Sagas;


public abstract class SagaData
{
    //@think: it's really stupid if saga will be used only  for order entity
    public Guid CorrelationId { get; set; }
}
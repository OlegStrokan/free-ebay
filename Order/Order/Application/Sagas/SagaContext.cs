namespace Application.Sagas;

public abstract class SagaContext
{
   public Dictionary<string, object> Metadata { get; set; } = new();
}
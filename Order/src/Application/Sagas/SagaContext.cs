namespace Application.Sagas;

public sealed class SagaContext
{
   public string? ReservationId { get; set; }
   public string? PaymentId { get; set; }
   public string? ShipmentId { get; set; }
   public Dictionary<string, object> Metadata { get; set; } = new();
}
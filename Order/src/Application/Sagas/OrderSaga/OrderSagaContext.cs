namespace Application.Sagas.OrderSaga;

public sealed class OrderSagaContext : SagaContext
{
    // external services IDs
    public string? ReservationId { get; set; }
    public string? PaymentId { get; set; }
    public string? ShipmentId { get; set; }
    
    // internal flags - for idempotency
    public bool OrderStatusUpdated { get; set; }
    public bool TrackingAssigned { get; set; }
}
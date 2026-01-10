namespace Application.Sagas.OrderSaga;

public sealed class OrderSagaContext : SagaContext
{
    public string? ReservationId { get; set; }
    public string? PaymentId { get; set; }
    public string? ShipmentId { get; set; }
}
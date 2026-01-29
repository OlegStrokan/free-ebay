namespace Application.Sagas.ReturnSaga;

public class ReturnSagaContext : SagaContext
{
    public string? ReturnShipmentId { get; set; }
    public string? RefundId { get; set; }
    public string? RevenueReversalId { get; set; }
    public DateTime? ReturnReceivedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? TrackingId { get; set; }
}
using Application.DTOs;

namespace Application.Sagas.ReturnSaga;

public class ReturnSagaData : SagaData
{
    public Guid CustomerId { get; set; }
    public string ReturnReason { get; set; } = string.Empty;
    public List<OrderItemDto> ReturnedItems { get; set; } = new();
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
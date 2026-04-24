using Application.Common;
using Application.DTOs;

namespace Application.Queries.GetPaymentByOrderId;

public sealed record GetPaymentByOrderIdQuery(
    string OrderId,
    string IdempotencyKey) : IQuery<Result<PaymentDetailsDto>>;
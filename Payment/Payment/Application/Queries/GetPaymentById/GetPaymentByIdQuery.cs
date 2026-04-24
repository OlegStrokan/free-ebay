using Application.Common;
using Application.DTOs;

namespace Application.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(string PaymentId) : IQuery<Result<PaymentDetailsDto>>;
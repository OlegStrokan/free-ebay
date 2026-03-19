using Application.Common;
using Application.DTOs;

namespace Application.Commands.RefundPayment;

public sealed record RefundPaymentCommand(
    string PaymentId,
    decimal Amount,
    string Currency,
    string Reason,
    string IdempotencyKey) : ICommand<Result<RefundPaymentResultDto>>;
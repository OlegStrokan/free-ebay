using Application.Common;
using Application.DTOs;

namespace Application.Commands.CapturePayment;

public sealed record CapturePaymentCommand(
    string OrderId,
    string CustomerId,
    string ProviderPaymentIntentId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : ICommand<Result<ProcessPaymentResultDto>>;

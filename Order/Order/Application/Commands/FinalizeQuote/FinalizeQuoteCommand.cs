using Application.Common;

namespace Application.Commands.FinalizeQuote;

public record FinalizeQuoteCommand(
    Guid B2BOrderId,
    string PaymentMethod,
    string IdempotencyKey) : ICommand<Result<Guid>>;

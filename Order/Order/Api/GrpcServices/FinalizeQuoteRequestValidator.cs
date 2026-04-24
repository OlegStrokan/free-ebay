using FluentValidation;
using Protos.Order;

namespace Api.GrpcServices;

public class FinalizeQuoteRequestValidator : AbstractValidator<FinalizeQuoteRequest>
{
    public FinalizeQuoteRequestValidator()
    {
        RuleFor(x => x.B2BOrderId)
            .NotEmpty().WithMessage("B2BOrderId is required")
            .Must(BeAGuid).WithMessage("Invalid B2BOrderId format");
        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("PaymentMethod is required");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

using FluentValidation;

namespace Application.Queries.GetPaymentByOrderId;

public sealed class GetPaymentByOrderIdQueryValidator : AbstractValidator<GetPaymentByOrderIdQuery>
{
    public GetPaymentByOrderIdQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required")
            .MaximumLength(128).WithMessage("IdempotencyKey must not exceed 128 characters");
    }
}
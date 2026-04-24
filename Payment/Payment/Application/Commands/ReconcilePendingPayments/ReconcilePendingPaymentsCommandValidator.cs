using FluentValidation;

namespace Application.Commands.ReconcilePendingPayments;

public sealed class ReconcilePendingPaymentsCommandValidator : AbstractValidator<ReconcilePendingPaymentsCommand>
{
    public ReconcilePendingPaymentsCommandValidator()
    {
        RuleFor(x => x.OlderThanMinutes)
            .GreaterThanOrEqualTo(1).WithMessage("OlderThanMinutes must be greater than or equal to 1.");

        RuleFor(x => x.BatchSize)
            .InclusiveBetween(1, 1000).WithMessage("BatchSize must be between 1 and 1000.");
    }
}
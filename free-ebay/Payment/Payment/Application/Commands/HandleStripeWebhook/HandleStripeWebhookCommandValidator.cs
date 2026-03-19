using FluentValidation;

namespace Application.Commands.HandleStripeWebhook;

public sealed class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator()
    {
        RuleFor(x => x.ProviderEventId)
            .NotEmpty().WithMessage("ProviderEventId is required.");

        RuleFor(x => x.EventType)
            .NotEmpty().WithMessage("EventType is required.");

        RuleFor(x => x.PayloadJson)
            .NotEmpty().WithMessage("PayloadJson is required.");

        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("PaymentId is required for this webhook outcome.")
            .When(x => x.Outcome is not StripeWebhookOutcome.Unknown);

        RuleFor(x => x.ProviderPaymentIntentId)
            .NotEmpty().WithMessage("ProviderPaymentIntentId is required for PaymentSucceeded outcome.")
            .When(x => x.Outcome == StripeWebhookOutcome.PaymentSucceeded);
    }
}
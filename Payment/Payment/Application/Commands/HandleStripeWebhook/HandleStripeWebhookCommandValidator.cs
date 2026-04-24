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

        RuleFor(x => x)
            .Must(HaveResolutionData)
            .WithMessage("PaymentId, ProviderPaymentIntentId, or ProviderRefundId is required for this webhook outcome.")
            .When(x => x.Outcome is not StripeWebhookOutcome.Unknown);
    }

    private static bool HaveResolutionData(HandleStripeWebhookCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.PaymentId)
               || !string.IsNullOrWhiteSpace(command.ProviderPaymentIntentId)
               || !string.IsNullOrWhiteSpace(command.ProviderRefundId);
    }
}
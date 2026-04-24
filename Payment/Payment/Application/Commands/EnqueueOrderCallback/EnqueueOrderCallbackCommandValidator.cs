using Application.Common;
using FluentValidation;

namespace Application.Commands.EnqueueOrderCallback;

public sealed class EnqueueOrderCallbackCommandValidator : AbstractValidator<EnqueueOrderCallbackCommand>
{
    public EnqueueOrderCallbackCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("PaymentId is required");

        RuleFor(x => x.RefundId)
            .NotEmpty().WithMessage("RefundId is required for refund callbacks")
            .When(x => x.CallbackType is OrderCallbackType.RefundSucceeded or OrderCallbackType.RefundFailed);
    }
}
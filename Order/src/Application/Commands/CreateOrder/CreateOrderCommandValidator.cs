namespace Application.Commands.CreateOrder;

using FluentValidation;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must have at least one item");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).NotEmpty();
                item.RuleFor(x => x.Quantity).GreaterThan(0);
                item.RuleFor(x => x.Price).GreaterThan(0);
            });

        RuleFor(x => x.DeliveryAddress).NotNull();
        RuleFor(x => x.DeliveryAddress.Street).NotEmpty();
        RuleFor(x => x.DeliveryAddress.City).NotEmpty();
        RuleFor(x => x.DeliveryAddress.Country).NotEmpty();
        RuleFor(x => x.DeliveryAddress.PostalCode).NotEmpty();

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            // todo: override with enum
            .Must(x => x == "stripe" || x == "crypto")
            .WithMessage("Payment method should be 'stripe' or 'crypto'");

        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
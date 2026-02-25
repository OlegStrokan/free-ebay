using FluentValidation;
using Protos.Order;

namespace Api.GrpcServices;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().Must(BeAGuid).WithMessage("Invalid CustomerId format");
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty();

        RuleFor(x => x.DeliveryAddress).NotNull();
        RuleSet("Address", () =>
        {
            RuleFor(x => x.DeliveryAddress.Street).NotEmpty();
            RuleFor(x => x.DeliveryAddress.City).NotEmpty();
            RuleFor(x => x.DeliveryAddress.Country).NotEmpty();
            RuleFor(x => x.DeliveryAddress.PostalCode).NotEmpty();
        });

        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().Must(BeAGuid);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.Price).GreaterThan(0);
            item.RuleFor(i => i.Currency).NotEmpty();
        });
    }

    private bool BeAGuid(string id) => Guid.TryParse(id, out _);
}
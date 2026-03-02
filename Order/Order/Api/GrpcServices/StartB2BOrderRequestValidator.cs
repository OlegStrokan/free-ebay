using FluentValidation;
using Protos.Order;

namespace Api.GrpcServices;

public class StartB2BOrderRequestValidator : AbstractValidator<StartB2BOrderRequest>
{
    public StartB2BOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().Must(BeAGuid).WithMessage("Invalid CustomerId format");
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.DeliveryAddress).NotNull().WithMessage("Delivery address is required");
        When(x => x.DeliveryAddress != null, () =>
        {
            RuleFor(x => x.DeliveryAddress.Street).NotEmpty();
            RuleFor(x => x.DeliveryAddress.City).NotEmpty();
            RuleFor(x => x.DeliveryAddress.Country).NotEmpty();
            RuleFor(x => x.DeliveryAddress.PostalCode).NotEmpty();
        });
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

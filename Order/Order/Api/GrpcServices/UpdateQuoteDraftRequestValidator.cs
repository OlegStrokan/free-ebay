using FluentValidation;
using Protos.Order;

namespace Api.GrpcServices;

public class UpdateQuoteDraftRequestValidator : AbstractValidator<UpdateQuoteDraftRequest>
{
    private static readonly HashSet<string> ValidChangeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "AddItem", "RemoveItem", "ChangeQuantity", "AdjustItemPrice"
        };

    public UpdateQuoteDraftRequestValidator()
    {
        RuleFor(x => x.B2BOrderId).NotEmpty().Must(IsGuid).WithMessage("Invalid B2BOrderId format");
        RuleFor(x => x.Changes).NotEmpty().WithMessage("At least one change is required");
        RuleForEach(x => x.Changes).ChildRules(c =>
        {
            c.RuleFor(i => i.ChangeType)
                .NotEmpty()
                .Must(t => ValidChangeTypes.Contains(t))
                .WithMessage("change_type must be AddItem, RemoveItem, ChangeQuantity, or AdjustItemPrice");

            c.RuleFor(i => i.ProductId).NotEmpty().Must(IsGuid).WithMessage("Invalid ProductId format");

            c.When(i => i.ChangeType is "AddItem" or "ChangeQuantity", () =>
                c.RuleFor(i => i.Quantity).GreaterThan(0));

            c.When(i => i.ChangeType is "AddItem" or "AdjustItemPrice", () =>
            {
                c.RuleFor(i => i.Currency).NotEmpty();
                c.RuleFor(i => i.Price)
                    .Must(p => p != null && (p.Units > 0 || p.Nanos > 0))
                    .WithMessage("Price must be greater than zero");
            });
        });
    }

    private static bool IsGuid(string id) => Guid.TryParse(id, out _);
}

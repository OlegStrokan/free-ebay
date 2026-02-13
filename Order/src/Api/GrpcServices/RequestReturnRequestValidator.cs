using FluentValidation;
using Protos.Orders;

namespace Api.GrpcServices;

public class RequestReturnRequestValidator : AbstractValidator<RequestReturnRequest>
{
    public RequestReturnRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().Must(id => Guid.TryParse(id, out _));
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.ItemToReturn).NotEmpty();
        RuleForEach(x => x.ItemToReturn).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().Must(id => Guid.TryParse(id, out _));
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
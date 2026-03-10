using FluentValidation;
using Protos.Product;

namespace Api.GrpcServices;

public class GetProductPricesRequestValidator : AbstractValidator<GetProductPricesRequest>
{
    public GetProductPricesRequestValidator()
    {
        RuleFor(x => x.ProductIds)
            .NotEmpty()
            .WithMessage("At least one product ID is required.");

        RuleForEach(x => x.ProductIds)
            .Must(BeAGuid)
            .WithMessage("Each product ID must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
{
    public GetProductsRequestValidator()
    {
        RuleFor(x => x.ProductIds)
            .NotEmpty()
            .WithMessage("At least one product ID is required.");

        RuleForEach(x => x.ProductIds)
            .Must(BeAGuid)
            .WithMessage("Each product ID must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetProductRequestValidator : AbstractValidator<GetProductRequest>
{
    public GetProductRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .Must(BeAGuid)
            .WithMessage("ProductId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

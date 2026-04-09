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

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty().Must(BeAGuid).WithMessage("SellerId must be a valid GUID.");
        RuleFor(x => x.CategoryId).NotEmpty().Must(BeAGuid).WithMessage("CategoryId must be a valid GUID.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-character ISO code.");
        RuleFor(x => x.Price).NotNull().WithMessage("Price is required.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAGuid).WithMessage("ProductId must be a valid GUID.");
        RuleFor(x => x.CategoryId).NotEmpty().Must(BeAGuid).WithMessage("CategoryId must be a valid GUID.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-character ISO code.");
        RuleFor(x => x.Price).NotNull().WithMessage("Price is required.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class DeleteProductRequestValidator : AbstractValidator<DeleteProductRequest>
{
    public DeleteProductRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAGuid).WithMessage("ProductId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class ActivateProductRequestValidator : AbstractValidator<ActivateProductRequest>
{
    public ActivateProductRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAGuid).WithMessage("ProductId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class DeactivateProductRequestValidator : AbstractValidator<DeactivateProductRequest>
{
    public DeactivateProductRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAGuid).WithMessage("ProductId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class UpdateProductStockRequestValidator : AbstractValidator<UpdateProductStockRequest>
{
    public UpdateProductStockRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAGuid).WithMessage("ProductId must be a valid GUID.");
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

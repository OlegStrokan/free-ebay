using FluentValidation;
using Protos.Product;

namespace Api.GrpcServices;

public class GetListingPricesRequestValidator : AbstractValidator<GetListingPricesRequest>
{
    public GetListingPricesRequestValidator()
    {
        RuleFor(x => x.ListingIds).NotEmpty().WithMessage("At least one listing ID is required.");
        RuleForEach(x => x.ListingIds).Must(BeAGuid).WithMessage("Each listing ID must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetListingsRequestValidator : AbstractValidator<GetListingsRequest>
{
    public GetListingsRequestValidator()
    {
        RuleFor(x => x.ListingIds).NotEmpty().WithMessage("At least one listing ID is required.");
        RuleForEach(x => x.ListingIds).Must(BeAGuid).WithMessage("Each listing ID must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetListingRequestValidator : AbstractValidator<GetListingRequest>
{
    public GetListingRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().Must(BeAGuid).WithMessage("ListingId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class CreateCatalogItemWithListingRequestValidator : AbstractValidator<CreateCatalogItemWithListingRequest>
{
    public CreateCatalogItemWithListingRequestValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty().Must(BeAGuid).WithMessage("SellerId must be a valid GUID.");
        RuleFor(x => x.CategoryId).NotEmpty().Must(BeAGuid).WithMessage("CategoryId must be a valid GUID.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-character ISO code.");
        RuleFor(x => x.Price).NotNull().WithMessage("Price is required.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class UpdateCatalogItemAndListingRequestValidator : AbstractValidator<UpdateCatalogItemAndListingRequest>
{
    public UpdateCatalogItemAndListingRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().Must(BeAGuid).WithMessage("ListingId must be a valid GUID.");
        RuleFor(x => x.CategoryId).NotEmpty().Must(BeAGuid).WithMessage("CategoryId must be a valid GUID.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-character ISO code.");
        RuleFor(x => x.Price).NotNull().WithMessage("Price is required.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class DeleteListingRequestValidator : AbstractValidator<DeleteListingRequest>
{
    public DeleteListingRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().Must(BeAGuid).WithMessage("ListingId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class ActivateListingRequestValidator : AbstractValidator<ActivateListingRequest>
{
    public ActivateListingRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().Must(BeAGuid).WithMessage("ListingId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class DeactivateListingRequestValidator : AbstractValidator<DeactivateListingRequest>
{
    public DeactivateListingRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().Must(BeAGuid).WithMessage("ListingId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class UpdateListingStockRequestValidator : AbstractValidator<UpdateListingStockRequest>
{
    public UpdateListingStockRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty().Must(BeAGuid).WithMessage("ListingId must be a valid GUID.");
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetProductPricesRequestValidator : AbstractValidator<GetProductPricesRequest>
{
    public GetProductPricesRequestValidator()
    {
        RuleFor(x => x.ProductIds).NotEmpty().WithMessage("At least one product ID is required.");
        RuleForEach(x => x.ProductIds).Must(BeAGuid).WithMessage("Each product ID must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
{
    public GetProductsRequestValidator()
    {
        RuleFor(x => x.ProductIds).NotEmpty().WithMessage("At least one product ID is required.");
        RuleForEach(x => x.ProductIds).Must(BeAGuid).WithMessage("Each product ID must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

public class GetProductRequestValidator : AbstractValidator<GetProductRequest>
{
    public GetProductRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().Must(BeAGuid).WithMessage("ProductId must be a valid GUID.");
    }

    private static bool BeAGuid(string id) => Guid.TryParse(id, out _);
}

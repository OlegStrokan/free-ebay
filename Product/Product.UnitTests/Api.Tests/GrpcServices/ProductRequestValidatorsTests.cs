using Api.GrpcServices;
using Protos.Product;

namespace Api.Tests.GrpcServices;

[TestFixture]
public class ProductRequestValidatorsTests
{

    [Test]
    public void GetProductPrices_Validate_EmptyList_ShouldHaveError()
    {
        var validator = new GetProductPricesRequestValidator();
        var result    = validator.Validate(new GetProductPricesRequest());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Select(e => e.PropertyName), Has.Member("ProductIds"));
    }

    [Test]
    public void GetProductPrices_Validate_InvalidGuid_ShouldHaveError()
    {
        var validator = new GetProductPricesRequestValidator();
        var request   = new GetProductPricesRequest { ProductIds = { "not-a-guid" } };

        var result = validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void GetProductPrices_Validate_ValidGuids_ShouldPass()
    {
        var validator = new GetProductPricesRequestValidator();
        var request   = new GetProductPricesRequest
        {
            ProductIds = { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var result = validator.Validate(request);

        Assert.That(result.IsValid, Is.True);
    }

    // ─── GetProductsRequestValidator ─────────────────────────────────────────

    [Test]
    public void GetProducts_Validate_EmptyList_ShouldHaveError()
    {
        var validator = new GetProductsRequestValidator();
        var result    = validator.Validate(new GetProductsRequest());

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void GetProducts_Validate_InvalidGuid_ShouldHaveError()
    {
        var validator = new GetProductsRequestValidator();
        var request   = new GetProductsRequest { ProductIds = { "bad" } };

        var result = validator.Validate(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void GetProducts_Validate_ValidGuids_ShouldPass()
    {
        var validator = new GetProductsRequestValidator();
        var request   = new GetProductsRequest { ProductIds = { Guid.NewGuid().ToString() } };

        Assert.That(validator.Validate(request).IsValid, Is.True);
    }

    // ─── GetProductRequestValidator ──────────────────────────────────────────

    [Test]
    public void GetProduct_Validate_EmptyProductId_ShouldHaveError()
    {
        var validator = new GetProductRequestValidator();
        var result    = validator.Validate(new GetProductRequest { ProductId = "" });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void GetProduct_Validate_InvalidGuid_ShouldHaveError()
    {
        var validator = new GetProductRequestValidator();
        var result    = validator.Validate(new GetProductRequest { ProductId = "not-a-guid" });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void GetProduct_Validate_ValidGuid_ShouldPass()
    {
        var validator = new GetProductRequestValidator();
        var result    = validator.Validate(new GetProductRequest { ProductId = Guid.NewGuid().ToString() });

        Assert.That(result.IsValid, Is.True);
    }
}

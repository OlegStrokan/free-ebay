using Api.GrpcServices;
using Api.Mappers;
using Application.Common;
using Application.DTOs;
using Application.Queries.GetProduct;
using Application.Queries.GetProductPrices;
using Application.Queries.GetProducts;
using Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Product;

namespace Api.Tests.GrpcServices;

[TestFixture]
public class ProductGrpcServiceTests
{
    private IMediator _mediator = null!;
    private ILogger<ProductGrpcService> _logger = null!;
    private IValidator<GetProductPricesRequest> _getPricesValidator = null!;
    private IValidator<GetProductsRequest> _getProductsValidator = null!;
    private IValidator<GetProductRequest> _getProductValidator = null!;
    private ServerCallContext _callContext = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<ProductGrpcService>>();
        _getPricesValidator = Substitute.For<IValidator<GetProductPricesRequest>>();
        _getProductsValidator = Substitute.For<IValidator<GetProductsRequest>>();
        _getProductValidator = Substitute.For<IValidator<GetProductRequest>>();
        _callContext = Substitute.For<ServerCallContext>();
    }

    private ProductGrpcService BuildService() =>
        new(_mediator, _logger, _getPricesValidator, _getProductsValidator, _getProductValidator);
    
    [Test]
    public async Task GetProductPrices_ShouldReturnPrices_WhenQuerySucceeds()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var request = new GetProductPricesRequest { ProductIds = { id1.ToString(), id2.ToString() } };

        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var prices = new List<ProductPriceDto>
        {
            new(id1, 10.00m, "USD"),
            new(id2, 20.00m, "EUR")
        };
        _mediator
            .Send(Arg.Any<GetProductPricesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProductPriceDto>>.Success(prices));

        var response = await BuildService().GetProductPrices(request, _callContext);

        Assert.That(response.Prices, Has.Count.EqualTo(2));
        Assert.That(response.Prices[0].ProductId, Is.EqualTo(id1.ToString()));
        Assert.That(response.Prices[1].Currency,  Is.EqualTo("EUR"));
    }

    [Test]
    public async Task GetProductPrices_ShouldSendCorrectIds_ToQuery()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var request = new GetProductPricesRequest { ProductIds = { id1.ToString(), id2.ToString() } };

        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductPricesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProductPriceDto>>.Success([]));

        await BuildService().GetProductPrices(request, _callContext);

        await _mediator.Received(1).Send(
            Arg.Is<GetProductPricesQuery>(q =>
                q.ProductIds.Contains(id1) && q.ProductIds.Contains(id2)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetProductPrices_ShouldThrowRpcException_WhenValidationFails()
    {
        var request = new GetProductPricesRequest { ProductIds = { "not-a-guid" } };

        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("ProductIds", "Invalid GUID") }));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProductPrices(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("Invalid GUID"));

        await _mediator.DidNotReceive().Send(Arg.Any<IRequest<Result<List<ProductPriceDto>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetProductPrices_ShouldThrowRpcException_WhenQueryFails()
    {
        var request = new GetProductPricesRequest { ProductIds = { Guid.NewGuid().ToString() } };

        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductPricesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProductPriceDto>>.Failure("DB error"));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProductPrices(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Internal));
    }

    [Test]
    public void GetProductPrices_ShouldThrowRpcException_WithInvalidArgument_WhenGuidFormatIsInvalid()
    {
        var request = new GetProductPricesRequest { ProductIds = { "bad-guid" } };

        // validation mocked to pass — FormatException thrown inside handler by Guid.Parse
        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProductPrices(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
    }

    [Test]
    public void GetProductPrices_ShouldThrowRpcException_WithInternal_WhenUnexpectedExceptionOccurs()
    {
        var request = new GetProductPricesRequest { ProductIds = { Guid.NewGuid().ToString() } };

        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductPricesQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected"));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProductPrices(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(ex.Status.Detail, Does.Contain("GetProductPrices"));
    }

    [Test]
    public void GetProductPrices_ShouldNotWrapExistingRpcException()
    {
        var request = new GetProductPricesRequest { ProductIds = { Guid.NewGuid().ToString() } };

        _getPricesValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var original = new RpcException(new Status(StatusCode.Unavailable, "service down"));
        _mediator
            .Send(Arg.Any<GetProductPricesQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(original);

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProductPrices(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Unavailable));
    }
    
    [Test]
    public async Task GetProducts_ShouldReturnProducts_WhenQuerySucceeds()
    {
        var id1 = Guid.NewGuid();
        var request = new GetProductsRequest { ProductIds = { id1.ToString() } };

        _getProductsValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var products = new List<ProductDetailDto>
        {
            SampleDetail(id1)
        };
        _mediator
            .Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProductDetailDto>>.Success(products));

        var response = await BuildService().GetProducts(request, _callContext);

        Assert.That(response.Products,    Has.Count.EqualTo(1));
        Assert.That(response.NotFoundIds, Is.Empty);
    }

    [Test]
    public async Task GetProducts_ShouldPopulateNotFoundIds_WhenSomeIdsNotReturned()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var request = new GetProductsRequest { ProductIds = { id1.ToString(), id2.ToString() } };

        _getProductsValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // only id1 returned — id2 should appear in not_found_ids
        _mediator
            .Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProductDetailDto>>.Success([SampleDetail(id1)]));

        var response = await BuildService().GetProducts(request, _callContext);

        Assert.That(response.Products,    Has.Count.EqualTo(1));
        Assert.That(response.NotFoundIds, Has.Count.EqualTo(1));
        Assert.That(response.NotFoundIds[0], Is.EqualTo(id2.ToString()));
    }

    [Test]
    public void GetProducts_ShouldThrowRpcException_WhenValidationFails()
    {
        var request = new GetProductsRequest { ProductIds = { "bad" } };

        _getProductsValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("ProductIds", "Invalid GUID") }));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProducts(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
    }

    [Test]
    public void GetProducts_ShouldThrowRpcException_WhenQueryFails()
    {
        var request = new GetProductsRequest { ProductIds = { Guid.NewGuid().ToString() } };

        _getProductsValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<ProductDetailDto>>.Failure("lookup error"));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProducts(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Internal));
    }

    [Test]
    public void GetProducts_ShouldThrowRpcException_WithInternal_WhenUnexpectedExceptionOccurs()
    {
        var request = new GetProductsRequest { ProductIds = { Guid.NewGuid().ToString() } };

        _getProductsValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("db down"));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProducts(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(ex.Status.Detail, Does.Contain("GetProducts"));
    }
    
    [Test]
    public async Task GetProduct_ShouldReturnProduct_WhenQuerySucceeds()
    {
        var id = Guid.NewGuid();
        var request = new GetProductRequest { ProductId = id.ToString() };

        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(id));

        var response = await BuildService().GetProduct(request, _callContext);

        Assert.That(response.Product.ProductId, Is.EqualTo(id.ToString()));
    }

    [Test]
    public void GetProduct_ShouldThrowRpcException_WhenValidationFails()
    {
        var request = new GetProductRequest { ProductId = "" };

        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("ProductId", "ProductId is required") }));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProduct(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Does.Contain("ProductId is required"));

        _mediator.DidNotReceive().Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetProduct_ShouldThrowRpcException_WhenGuidFormatIsInvalid()
    {
        var request = new GetProductRequest { ProductId = "not-a-guid" };
        
        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProduct(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
    }

    [Test]
    public void GetProduct_ShouldThrowRpcException_WithNotFound_WhenProductNotFound()
    {
        var id = Guid.NewGuid();
        var request = new GetProductRequest { ProductId = id.ToString() };

        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProductNotFoundException(id));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProduct(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.NotFound));
    }

    [Test]
    public async Task GetProduct_ShouldSendCorrectId_ToQuery()
    {
        var id = Guid.NewGuid();
        var request = new GetProductRequest { ProductId = id.ToString() };

        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(SampleDetail(id));

        await BuildService().GetProduct(request, _callContext);

        await _mediator.Received(1).Send(
            Arg.Is<GetProductQuery>(q => q.ProductId == id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetProduct_ShouldThrowRpcException_WithInternal_WhenUnexpectedExceptionOccurs()
    {
        var id = Guid.NewGuid();
        var request = new GetProductRequest { ProductId = id.ToString() };

        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected"));

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProduct(request, _callContext));

        Assert.That(ex!.StatusCode,    Is.EqualTo(StatusCode.Internal));
        Assert.That(ex.Status.Detail,  Does.Contain("GetProduct"));
    }

    [Test]
    public void GetProduct_ShouldNotWrapExistingRpcException()
    {
        var id = Guid.NewGuid();
        var request = new GetProductRequest { ProductId = id.ToString() };

        _getProductValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var original = new RpcException(new Status(StatusCode.Unavailable, "service unavailable"));
        _mediator
            .Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(original);

        var ex = Assert.ThrowsAsync<RpcException>(() =>
            BuildService().GetProduct(request, _callContext));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Unavailable));
    }
    
    private static ProductDetailDto SampleDetail(Guid id) => new(
        ProductId: id,
        Name:"Widget",
        Description:"A test widget",
        CategoryId: Guid.NewGuid(),
        CategoryName: "Tools",
        Price: 19.99m,
        Currency: "USD",
        StockQuantity: 50,
        Status: "Active",
        SellerId: Guid.NewGuid(),
        Attributes: [],
        ImageUrls: [],
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: null);
}



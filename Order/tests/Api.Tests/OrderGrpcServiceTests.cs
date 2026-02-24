using Api.GrpcServices;
using Application.Commands.CreateOrder;
using Application.Commands.RequestReturn;
using Application.Common;
using Application.DTOs;
using FluentValidation;
using FluentValidation.Results;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Order;

namespace Api.Tests;

public class OrderGrpcServiceTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<OrderGrpcService> _logger = Substitute.For<ILogger<OrderGrpcService>>();
    private readonly IValidator<CreateOrderRequest> _createValidator = Substitute.For<IValidator<CreateOrderRequest>>();
    private readonly IValidator<RequestReturnRequest> _returnValidator = Substitute.For<IValidator<RequestReturnRequest>>();
    private readonly ServerCallContext _callContext = Substitute.For<ServerCallContext>();

    private OrderGrpcService BuildService() =>
        new(_mediator, _logger, _createValidator, _returnValidator);

    [Fact]
    public async Task CreateOrder_ShouldReturnSuccess_WhenCommandSucceeds()
    {
        var orderId = Guid.NewGuid();
        var request = ValidCreateOrderRequest();

        _createValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(orderId));

        var response = await BuildService().CreateOrder(request, _callContext);

        Assert.True(response.Success);
        Assert.Equal(orderId.ToString(), response.OrderId);
        Assert.Empty(response.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnFailure_WhenValidationFails()
    {
        var request = ValidCreateOrderRequest();
        var failure = new ValidationFailure("CustomerId", "CustomerId is required");

        _createValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure }));

        var response = await BuildService().CreateOrder(request, _callContext);

        Assert.False(response.Success);
        Assert.Contains("CustomerId is required", response.ErrorMessage);

        await _mediator.DidNotReceive().Send(Arg.Any<IRequest<Result<Guid>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnFailure_WhenCommandFails()
    {
        var request = ValidCreateOrderRequest();

        _createValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Inventory not available"));

        var response = await BuildService().CreateOrder(request, _callContext);

        Assert.False(response.Success);
        Assert.Equal("Inventory not available", response.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrder_ShouldThrowRpcException_WithInvalidArgument_WhenGuidIsInvalidFormat()
    {
        // validation is mocked to pass, but Guid.Parse in MapToCreateCommand will throw FormatException
        var request = ValidCreateOrderRequest();
        request.CustomerId = "not-a-guid";

        _createValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult()); // bypass validation intentionally

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().CreateOrder(request, _callContext));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains("Invalid ID format", ex.Status.Detail);
    }

    [Fact]
    public async Task CreateOrder_ShouldThrowRpcException_WithInternal_WhenUnexpectedExceptionOccurs()
    {
        var request = ValidCreateOrderRequest();

        _createValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database connection lost"));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().CreateOrder(request, _callContext));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.Contains("CreateOrder", ex.Status.Detail);
    }

    [Fact]
    public async Task CreateOrder_ShouldPassMappedCommandToMediator()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = customerId.ToString(),
            IdempotencyKey = "idem-key",
            PaymentMethod = "CARD",
            DeliveryAddress = new Address
            {
                Street = "Main St", City = "Prague", Country = "CZ", PostalCode = "11000"
            },
            Items = { new OrderItem { ProductId = productId.ToString(), Quantity = 2, Price = 50.0, Currency = "USD" } }
        };

        _createValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await BuildService().CreateOrder(request, _callContext);

        await _mediator.Received(1).Send(
            Arg.Is<CreateOrderCommand>(c =>
                c.CustomerId == customerId &&
                c.IdempotencyKey == "idem-key" &&
                c.PaymentMethod == "CARD" &&
                c.Items.Count == 1 &&
                c.Items[0].ProductId == productId),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task RequestReturn_ShouldReturnSuccess_WhenCommandSucceeds()
    {
        var returnRequestId = Guid.NewGuid();
        var request = ValidRequestReturnRequest();

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<RequestReturnCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(returnRequestId));

        var response = await BuildService().RequestReturn(request, _callContext);

        Assert.True(response.Success);
        Assert.Equal(returnRequestId.ToString(), response.OrderId);
    }

    [Fact]
    public async Task RequestReturn_ShouldReturnFailure_WhenValidationFails()
    {
        var request = ValidRequestReturnRequest();
        var failure = new ValidationFailure("Reason", "Reason is required");

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure }));

        var response = await BuildService().RequestReturn(request, _callContext);

        Assert.False(response.Success);
        Assert.Contains("Reason is required", response.ErrorMessage);

        await _mediator.DidNotReceive().Send(Arg.Any<IRequest<Result<Guid>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestReturn_ShouldReturnFailure_WhenCommandFails()
    {
        var request = ValidRequestReturnRequest();

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<RequestReturnCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Return window expired"));

        var response = await BuildService().RequestReturn(request, _callContext);

        Assert.False(response.Success);
        Assert.Equal("Return window expired", response.ErrorMessage);
    }

    [Fact]
    public async Task RequestReturn_ShouldThrowRpcException_WithInvalidArgument_WhenOrderIdIsInvalidGuid()
    {
        var request = ValidRequestReturnRequest();
        request.OrderId = "bad-guid";

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult()); // bypass validation

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().RequestReturn(request, _callContext));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains("Invalid ID format", ex.Status.Detail);
    }

    [Fact]
    public async Task RequestReturn_ShouldThrowRpcException_WithInternal_WhenUnexpectedExceptionOccurs()
    {
        var request = ValidRequestReturnRequest();

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<RequestReturnCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().RequestReturn(request, _callContext));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.Contains("RequestReturn", ex.Status.Detail);
    }

    [Fact]
    public async Task RequestReturn_ShouldPassMappedCommandToMediator()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new RequestReturnRequest
        {
            OrderId = orderId.ToString(),
            Reason = "Defective product",
            IdempotencyKey = "idem-return",
            ItemsToReturn = { new OrderItem { ProductId = productId.ToString(), Quantity = 1, Price = 100.0, Currency = "USD" } }
        };

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mediator
            .Send(Arg.Any<RequestReturnCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await BuildService().RequestReturn(request, _callContext);

        await _mediator.Received(1).Send(
            Arg.Is<RequestReturnCommand>(c =>
                c.OrderId == orderId &&
                c.Reason == "Defective product" &&
                c.IdempotencyKey == "idem-return" &&
                c.ItemsToReturn.Count == 1 &&
                c.ItemsToReturn[0].ProductId == productId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestReturn_ShouldNotPropagateRpcException_WhenAlreadyRpcException()
    {
        var request = ValidRequestReturnRequest();

        _returnValidator
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var rpcEx = new RpcException(new Status(StatusCode.Unavailable, "service down"));
        _mediator
            .Send(Arg.Any<RequestReturnCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(rpcEx);

        // service does not transform RpcException when the mediator already threw one. it should propagate the same exception untouched
        var thrown = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().RequestReturn(request, _callContext));

        Assert.Equal(StatusCode.Unavailable, thrown.StatusCode);
    }

    // helpers 
    private static CreateOrderRequest ValidCreateOrderRequest() => new()
    {
        CustomerId = Guid.NewGuid().ToString(),
        IdempotencyKey = Guid.NewGuid().ToString(),
        PaymentMethod = "CARD",
        DeliveryAddress = new Address
        {
            Street = "Baker St", City = "London", Country = "UK", PostalCode = "NW1"
        },
        Items = { new OrderItem { ProductId = Guid.NewGuid().ToString(), Quantity = 1, Price = 99.99, Currency = "USD" } }
    };

    private static RequestReturnRequest ValidRequestReturnRequest() => new()
    {
        OrderId = Guid.NewGuid().ToString(),
        Reason = "Damaged in transit",
        IdempotencyKey = Guid.NewGuid().ToString(),
        ItemsToReturn = { new OrderItem { ProductId = Guid.NewGuid().ToString(), Quantity = 1, Price = 99.99, Currency = "USD" } }
    };
}

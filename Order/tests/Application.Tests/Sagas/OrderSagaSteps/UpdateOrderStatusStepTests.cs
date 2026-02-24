using Application.DTOs;
using Application.Interfaces;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Exceptions;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class UpdateOrderStatusStepTests
{
    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly ILogger<UpdateOrderStatusStep> _logger =
        Substitute.For<ILogger<UpdateOrderStatusStep>>();

    private UpdateOrderStatusStep BuildStep() =>
        new(_orderPersistenceService, _logger);
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOrderIsUpdated()
    {
        var context = new OrderSagaContext { PaymentId = "PAY-123" };
        var data = CreateSampleData();

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Paid", result.Data?["Status"]);
        Assert.True(context.OrderStatusUpdated);

        await _orderPersistenceService.Received(1).UpdateOrderAsync(
            data.CorrelationId,
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkip_WhenOrderStatusAlreadyUpdated_Idempotency()
    {
        var context = new OrderSagaContext { PaymentId = "PAY-123", OrderStatusUpdated = true };
        var data = CreateSampleData();

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);

        await _orderPersistenceService.DidNotReceive().UpdateOrderAsync(
            Arg.Any<Guid>(), Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentIdMissingInContext()
    {
        var context = new OrderSagaContext { PaymentId = null };
        var data = CreateSampleData();

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Payment ID not found", result.ErrorMessage);

        // persistence must not be touched when context is incomplete
        await _orderPersistenceService.DidNotReceive().UpdateOrderAsync(
            Arg.Any<Guid>(), Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var context = new OrderSagaContext { PaymentId = "PAY-123" };
        var data = CreateSampleData();

        _orderPersistenceService
            .UpdateOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new OrderNotFoundException(data.CorrelationId));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Critical Error", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        var context = new OrderSagaContext { PaymentId = "PAY-123" };
        var data = CreateSampleData();

        _orderPersistenceService
            .UpdateOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database timeout"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Database timeout", result.ErrorMessage);
    }
    
    [Fact]
    public async Task CompensateAsync_ShouldCancelOrder_WhenOrderExists()
    {
        var data = CreateSampleData();

        await BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _orderPersistenceService.Received(1).UpdateOrderAsync(
            data.CorrelationId,
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_WhenOrderNotFound()
    {
        _orderPersistenceService
            .UpdateOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new OrderNotFoundException(Guid.NewGuid()));

        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_WhenUnexpectedExceptionOccurs()
    {
        _orderPersistenceService
            .UpdateOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("DB crashed"));

        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None));

        Assert.Null(exception);
    }
    
    private static OrderSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
        Items = new List<OrderItemDto>()
    };
}
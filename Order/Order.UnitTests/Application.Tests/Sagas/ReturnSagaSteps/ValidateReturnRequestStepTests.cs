using Application.DTOs;
using Application.Interfaces;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Services;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.ReturnSagaSteps;

public class ValidateReturnRequestStepTests
{
    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly IReturnRequestPersistenceService _returnRequestPersistenceService =
        Substitute.For<IReturnRequestPersistenceService>();

    private readonly ReturnPolicyService _returnPolicyService = new();
    private readonly ILogger<ValidateReturnRequestStep> _logger =
        Substitute.For<ILogger<ValidateReturnRequestStep>>();

    private ValidateReturnRequestStep BuildStep() => new(
        _orderPersistenceService,
        _returnRequestPersistenceService,
        _returnPolicyService,
        _logger);
        
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOrderIsCompletedAndEligible()
    {
        var order = CreateCompleteOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        _returnRequestPersistenceService
            .LoadByOrderIdAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns((ReturnRequest?)null);

        var context = new ReturnSagaContext();
        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(data.CorrelationId, result.Data?["OrderId"]);
        Assert.True(context.ReturnRequestValidated);

        await _returnRequestPersistenceService.Received(1).CreateReturnRequestAsync(
            Arg.Any<ReturnRequest>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkip_WhenAlreadyValidated_Idempotency()
    {
        var context = new ReturnSagaContext { ReturnRequestValidated = true };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(Guid.NewGuid()), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(true, result.Data?["Idempotent"]);

        await _orderPersistenceService.DidNotReceive()
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenReturnRequestAlreadyExists()
    {
        var order = CreateCompleteOrder();
        
        // idempotency test
        var data = CreateSampleData(order.Id.Value);
        var existingRequest = CreateReturnRequest(order);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        _returnRequestPersistenceService
            .LoadByOrderIdAsync(order.Id.Value, Arg.Any<CancellationToken>())
            .Returns(existingRequest);

        var context = new ReturnSagaContext();
        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("ExistingRecord", result.Data?["Source"]);
        Assert.True(context.ReturnRequestValidated);

        await _returnRequestPersistenceService.DidNotReceive().CreateReturnRequestAsync(
            Arg.Any<ReturnRequest>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData(Guid.NewGuid());

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderStatusIsNotCompleted()
    {
        var order = CreatePendingOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("must be completed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotEligibleForReturn()
    {
        // order completed, but return window has passed (simulate by creating order with old timestamp)
        var order = CreateCompleteOrder(DateTime.UtcNow.AddDays(-20));
        var data = CreateSampleData(order.Id.Value);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        _returnRequestPersistenceService
            .LoadByOrderIdAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns((ReturnRequest?)null);

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not eligible", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        var data = CreateSampleData(Guid.NewGuid());

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("DB connection lost"));

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("DB connection lost", result.ErrorMessage);
    }

    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_ValidateIsNotReversible()
    {
        var data = CreateSampleData(Guid.NewGuid());

        await BuildStep().CompensateAsync(data, new ReturnSagaContext(), CancellationToken.None);

        await _orderPersistenceService.DidNotReceive()
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _returnRequestPersistenceService.DidNotReceive()
            .UpdateReturnRequestAsync(Arg.Any<Guid>(), Arg.Any<Func<ReturnRequest, Task>>(), Arg.Any<CancellationToken>());
    }

    private static ReturnSagaData CreateSampleData(Guid correlationId) => new()
    {
        CorrelationId = correlationId,
        Currency = "USD",
        CustomerId = Guid.NewGuid(),
        RefundAmount = 100m,
        ReturnedItems = new List<OrderItemDto> { new(Guid.NewGuid(), 1, 100m, "USD") },
        ReturnReason = "Defective item"
    };

    private static Order CreatePendingOrder()
    {
        var customerId = CustomerId.CreateUnique();
        var address = Address.Create("Baker St", "London", "UK", "NW1");
        var items = new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) };
        return Order.Create(customerId, address, items);
    }

    private static Order CreateCompleteOrder(DateTime? completedAt = null)
    {
        var order = CreatePendingOrder();
        order.Pay(PaymentId.From("PAY-123"));
        order.Approve();
        order.Complete();
        
        if (completedAt.HasValue)
        {
            // use reflection to set the private _completedAt field for testing
            var field = typeof(Order).GetField("_completedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(order, completedAt.Value);
        }
        
        order.ClearUncommittedEvents();
        return order;
    }

    private static ReturnRequest CreateReturnRequest(Order order) =>
        ReturnRequest.Create(
            orderId: order.Id,
            customerId: CustomerId.CreateUnique(),
            reason: "Defective",
            itemsToReturn: order.Items.ToList(),
            refundAmount: Money.Create(100, "USD"),
            orderCompletedAt: order.CompletedAt!.Value,
            orderItems: order.Items.ToList(),
            returnWindow: TimeSpan.FromDays(14));
}
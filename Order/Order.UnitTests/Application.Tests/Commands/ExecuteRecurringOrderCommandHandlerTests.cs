using Application.Commands.RecurringOrder.ExecuteRecurringOrder;
using Application.Interfaces;
using Domain.Entities.Subscription;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class ExecuteRecurringOrderCommandHandlerTests
{
    private readonly IRecurringOrderPersistenceService _persistenceService =
        Substitute.For<IRecurringOrderPersistenceService>();

    private readonly ILogger<ExecuteRecurringOrderCommandHandler> _logger =
        Substitute.For<ILogger<ExecuteRecurringOrderCommandHandler>>();

    private ExecuteRecurringOrderCommandHandler BuildHandler() =>
        new(_persistenceService, _logger);

    private static RecurringOrder BuildDueOrder(DateTime? firstRunAt = null)
    {
        var items = new List<RecurringOrderItem>
        {
            RecurringOrderItem.Create(ProductId.CreateUnique(), 2, Money.Create(50m, "USD"))
        };
        return RecurringOrder.Create(
            CustomerId.CreateUnique(),
            ScheduleFrequency.Weekly,
            items,
            Address.Create("Main St", "Prague", "CZ", "11000"),
            "Card-123",
            firstRunAt: firstRunAt ?? DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithChildOrderId_WhenDue()
    {
        var order = BuildDueOrder();
        var childOrderId = Guid.NewGuid();

        _persistenceService
            .LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);
        _persistenceService
            .ExecuteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(childOrderId);

        var result = await BuildHandler().Handle(
            new ExecuteRecurringOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(childOrderId, result.Value);

        await _persistenceService.Received(1).ExecuteAsync(
            Arg.Is<Guid>(g => g == order.Id.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithEmptyGuid_WhenOrderIsNotDue()
    {
        // NextRunAt is in the future → IsDue = false
        var order = BuildDueOrder(firstRunAt: DateTime.UtcNow.AddDays(1));

        _persistenceService
            .LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await BuildHandler().Handle(
            new ExecuteRecurringOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Value);

        // ExecuteAsync must NOT be called when order is not due (concurrent scheduler guard)
        await _persistenceService.DidNotReceive().ExecuteAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRecurringOrderNotFound()
    {
        _persistenceService
            .LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((RecurringOrder?)null);

        var result = await BuildHandler().Handle(
            new ExecuteRecurringOrderCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenExecuteAsyncThrows()
    {
        var order = BuildDueOrder();

        _persistenceService
            .LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);
        _persistenceService
            .ExecuteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Execution pipeline error"));

        var result = await BuildHandler().Handle(
            new ExecuteRecurringOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Execution pipeline error", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldNotCallExecuteAsync_WhenOrderIsPaused()
    {
        // Create active order, then pause it so IsDue returns false
        var items = new List<RecurringOrderItem>
        {
            RecurringOrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(10m, "USD"))
        };
        var order = RecurringOrder.Create(
            CustomerId.CreateUnique(),
            ScheduleFrequency.Weekly,
            items,
            Address.Create("St", "City", "CZ", "11000"),
            "Card",
            firstRunAt: DateTime.UtcNow.AddMinutes(-1)); // past, but about to be paused
        order.Pause();

        _persistenceService
            .LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await BuildHandler().Handle(
            new ExecuteRecurringOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Value);

        await _persistenceService.DidNotReceive().ExecuteAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

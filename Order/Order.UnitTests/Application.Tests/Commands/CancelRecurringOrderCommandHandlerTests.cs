using Application.Commands.RecurringOrder.CancelRecurringOrder;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class CancelRecurringOrderCommandHandlerTests
{
    private readonly IRecurringOrderPersistenceService _persistenceService =
        Substitute.For<IRecurringOrderPersistenceService>();

    private readonly ILogger<CancelRecurringOrderCommandHandler> _logger =
        Substitute.For<ILogger<CancelRecurringOrderCommandHandler>>();

    private CancelRecurringOrderCommandHandler BuildHandler() =>
        new(_persistenceService, _logger);

    private static CancelRecurringOrderCommand ValidCommand(Guid? id = null) =>
        new(RecurringOrderId: id ?? Guid.NewGuid(), Reason: "Budget cut");

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOrderIsCancelled()
    {
        _persistenceService
            .UpdateAsync(Arg.Any<Guid>(),
                Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await _persistenceService.Received(1).UpdateAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainExceptionIsThrown()
    {
        _persistenceService
            .UpdateAsync(Arg.Any<Guid>(),
                Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
                Arg.Any<CancellationToken>())
            .Throws(new DomainException("Cannot transition RecurringOrder from 'Cancelled'"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot transition", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUnexpectedExceptionIsThrown()
    {
        _persistenceService
            .UpdateAsync(Arg.Any<Guid>(),
                Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Timeout"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Timeout", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldForwardCorrectId_ToPersistenceService()
    {
        var id = Guid.NewGuid();
        _persistenceService
            .UpdateAsync(Arg.Any<Guid>(),
                Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await BuildHandler().Handle(ValidCommand(id), CancellationToken.None);

        await _persistenceService.Received(1).UpdateAsync(
            Arg.Is<Guid>(g => g == id),
            Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
            Arg.Any<CancellationToken>());
    }
}

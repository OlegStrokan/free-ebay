using Application.Commands.RecurringOrder.PauseRecurringOrder;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class PauseRecurringOrderCommandHandlerTests
{
    private readonly IRecurringOrderPersistenceService _persistenceService =
        Substitute.For<IRecurringOrderPersistenceService>();

    private readonly ILogger<PauseRecurringOrderCommandHandler> _logger =
        Substitute.For<ILogger<PauseRecurringOrderCommandHandler>>();

    private PauseRecurringOrderCommandHandler BuildHandler() =>
        new(_persistenceService, _logger);

    private static PauseRecurringOrderCommand ValidCommand(Guid? id = null) =>
        new(RecurringOrderId: id ?? Guid.NewGuid());

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOrderIsPaused()
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
            .Throws(new DomainException("Cannot transition RecurringOrder from 'Paused' to 'Paused'"));

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
            .Throws(new Exception("Connection reset"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Connection reset", result.Error);
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

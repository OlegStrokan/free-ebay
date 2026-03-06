using Application.Commands.RecurringOrder.ResumeRecurringOrder;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class ResumeRecurringOrderCommandHandlerTests
{
    private readonly IRecurringOrderPersistenceService _persistenceService =
        Substitute.For<IRecurringOrderPersistenceService>();

    private readonly ILogger<ResumeRecurringOrderCommandHandler> _logger =
        Substitute.For<ILogger<ResumeRecurringOrderCommandHandler>>();

    private ResumeRecurringOrderCommandHandler BuildHandler() =>
        new(_persistenceService, _logger);

    private static ResumeRecurringOrderCommand ValidCommand(Guid? id = null) =>
        new(RecurringOrderId: id ?? Guid.NewGuid());

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOrderIsResumed()
    {
        _persistenceService
            .UpdateAsync(Arg.Any<Guid>(),
                Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainExceptionIsThrown()
    {
        _persistenceService
            .UpdateAsync(Arg.Any<Guid>(),
                Arg.Any<Func<Domain.Entities.Subscription.RecurringOrder, Task>>(),
                Arg.Any<CancellationToken>())
            .Throws(new DomainException("Cannot transition RecurringOrder from 'Active' to 'Active'"));

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
            .Throws(new Exception("Storage unavailable"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Storage unavailable", result.Error);
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

using Application.Commands.CancelB2BOrder;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class CancelB2BOrderCommandHandlerTests
{
    private readonly IB2BOrderPersistenceService _persistenceService =
        Substitute.For<IB2BOrderPersistenceService>();

    private readonly ILogger<CancelB2BOrderCommandHandler> _logger =
        Substitute.For<ILogger<CancelB2BOrderCommandHandler>>();

    private CancelB2BOrderCommandHandler BuildHandler() =>
        new(_persistenceService, _logger);

    private static CancelB2BOrderCommand ValidCommand(Guid? id = null) =>
        new(
            B2BOrderId: id ?? Guid.NewGuid(),
            Reasons: new List<string> { "Budget cut", "Vendor changed" });


    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOrderIsCancelled()
    {
        _persistenceService
            .UpdateB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await _persistenceService.Received(1).UpdateB2BOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainExceptionIsThrown()
    {
        _persistenceService
            .UpdateB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new DomainException("Cannot cancel a finalized quote"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot cancel a finalized quote", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUnexpectedExceptionIsThrown()
    {
        _persistenceService
            .UpdateB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Network timeout"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Network timeout", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldForwardCorrectB2BOrderId_ToPersistenceService()
    {
        var id = Guid.NewGuid();
        _persistenceService
            .UpdateB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await BuildHandler().Handle(ValidCommand(id), CancellationToken.None);

        await _persistenceService.Received(1).UpdateB2BOrderAsync(
            Arg.Is<Guid>(g => g == id),
            Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(),
            Arg.Any<CancellationToken>());
    }
}

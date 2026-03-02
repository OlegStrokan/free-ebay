using Application.Commands.UpdateQuoteDraft;
using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class UpdateQuoteDraftCommandHandlerTests
{
    private readonly IB2BOrderPersistenceService _persistenceService =
        Substitute.For<IB2BOrderPersistenceService>();

    private readonly ILogger<UpdateQuoteDraftCommandHandler> _logger =
        Substitute.For<ILogger<UpdateQuoteDraftCommandHandler>>();

    private UpdateQuoteDraftCommandHandler BuildHandler() =>
        new(_persistenceService, _logger);

    private static UpdateQuoteDraftCommand ValidCommand(Guid? b2bOrderId = null) =>
        new(
            B2BOrderId: b2bOrderId ?? Guid.NewGuid(),
            Changes: new List<QuoteItemChangeDto>
            {
                new(QuoteChangeType.AddItem, Guid.NewGuid(), 2, 50m, "USD")
            },
            Comment: null,
            CommentAuthor: null);

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenChangesAreApplied()
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
            .Throws(new DomainException("Product not found in quote"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Product not found in quote", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUnexpectedExceptionIsThrown()
    {
        _persistenceService
            .UpdateB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<Domain.Entities.B2BOrder.B2BOrder, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Kafka on fire"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Kafka on fire, me too", result.Error);
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

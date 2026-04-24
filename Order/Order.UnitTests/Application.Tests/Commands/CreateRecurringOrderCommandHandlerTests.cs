using Application.Commands.RecurringOrder.CreateRecurringOrder;
using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Subscription;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class CreateRecurringOrderCommandHandlerTests
{
    private readonly IRecurringOrderPersistenceService _persistenceService =
        Substitute.For<IRecurringOrderPersistenceService>();

    private readonly IIdempotencyRepository _idempotencyRepository =
        Substitute.For<IIdempotencyRepository>();

    private readonly IUserGateway _userGateway =
        Substitute.For<IUserGateway>();

    private readonly ILogger<CreateRecurringOrderCommandHandler> _logger =
        Substitute.For<ILogger<CreateRecurringOrderCommandHandler>>();

    private CreateRecurringOrderCommandHandler BuildHandler() =>
        new(_persistenceService, _idempotencyRepository, _userGateway, _logger);

    private void SetupUserGateway(bool isActive = true)
    {
        _userGateway
            .GetUserProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var customerId = callInfo.ArgAt<Guid>(0);
                return Task.FromResult(new UserProfileDto(
                    customerId,
                    $"{customerId}@test.local",
                    "Test Customer",
                    "US",
                    "Standard",
                    isActive));
            });
    }

    private static CreateRecurringOrderCommand ValidCommand(string idempotencyKey = "idem-recurring-001") =>
        new(
            CustomerId: Guid.NewGuid(),
            PaymentMethod: "Card-123",
            Frequency: "Weekly",
            Items: new List<RecurringItemDto> { new(Guid.NewGuid(), 2, 50m, "USD") },
            DeliveryAddress: new AddressDto("Na Příkopě", "Prague", "CZ", "11000"),
            FirstRunAt: null,
            MaxExecutions: null,
            IdempotencyKey: idempotencyKey);

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithNewId_WhenOrderIsCreated()
    {
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        await _persistenceService.Received(1).CreateAsync(
            Arg.Any<RecurringOrder>(),
            Arg.Is<string>(k => k == "idem-recurring-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingId_WhenDuplicateIdempotencyKey()
    {
        var existingId = Guid.NewGuid();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyRecord("idem-recurring-001", existingId, DateTime.UtcNow));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingId, result.Value);

        await _persistenceService.DidNotReceive().CreateAsync(
            Arg.Any<RecurringOrder>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _userGateway.DidNotReceive().GetUserProfileAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPersistenceThrows()
    {
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _persistenceService
            .CreateAsync(Arg.Any<RecurringOrder>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("DB write failed"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("DB write failed", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectIdempotencyKey_ToPersistenceService()
    {
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        await BuildHandler().Handle(ValidCommand("my-special-key"), CancellationToken.None);

        await _persistenceService.Received(1).CreateAsync(
            Arg.Any<RecurringOrder>(),
            Arg.Is<string>(k => k == "my-special-key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCustomerIsBlocked()
    {
        SetupUserGateway(isActive: false);
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("blocked", result.Error, StringComparison.OrdinalIgnoreCase);

        await _persistenceService.DidNotReceive().CreateAsync(
            Arg.Any<RecurringOrder>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}

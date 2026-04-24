using Application.Commands.StartB2BOrder;
using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class StartB2BOrderCommandHandlerTests
{
    private readonly IB2BOrderPersistenceService _persistenceService =
        Substitute.For<IB2BOrderPersistenceService>();

    private readonly IIdempotencyRepository _idempotencyRepository =
        Substitute.For<IIdempotencyRepository>();

    private readonly IUserGateway _userGateway =
        Substitute.For<IUserGateway>();

    private readonly ILogger<StartB2BOrderCommandHandler> _logger =
        Substitute.For<ILogger<StartB2BOrderCommandHandler>>();

    private StartB2BOrderCommandHandler BuildHandler() =>
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

    private static StartB2BOrderCommand ValidCommand(string idempotencyKey = "idem-start-001") =>
        new(
            CustomerId: Guid.NewGuid(),
            CompanyName: "ACME Corp",
            DeliveryAddress: new AddressDto("Baker Stritko", "London", "UK", "NW1"),
            IdempotencyKey: idempotencyKey);

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithNewId_WhenOrderIsStarted()
    {
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        await _persistenceService.Received(1).StartB2BOrderAsync(
            Arg.Any<Domain.Entities.B2BOrder.B2BOrder>(),
            Arg.Is<string>(k => k == "idem-start-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingId_WhenDuplicateIdempotencyKey()
    {
        var existingId = Guid.NewGuid();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyRecord("idem-start-dup", existingId, DateTime.UtcNow));

        var result = await BuildHandler().Handle(ValidCommand("idem-start-dup"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingId, result.Value);

        await _persistenceService.DidNotReceive().StartB2BOrderAsync(
            Arg.Any<Domain.Entities.B2BOrder.B2BOrder>(),
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
            .StartB2BOrderAsync(
                Arg.Any<Domain.Entities.B2BOrder.B2BOrder>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Postgres is having a bad day"));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Postgres is having a bad day", result.Error);
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

        await _persistenceService.DidNotReceive().StartB2BOrderAsync(
            Arg.Any<Domain.Entities.B2BOrder.B2BOrder>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}

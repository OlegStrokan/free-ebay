using Application.Commands.FinalizeQuote;
using Application.Interfaces;
using Domain.Entities.B2BOrder;
using Domain.Entities.Order;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class FinalizeQuoteCommandHandlerTests
{
    private readonly IB2BOrderPersistenceService _b2bPersistenceService =
        Substitute.For<IB2BOrderPersistenceService>();

    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly IIdempotencyRepository _idempotencyRepository =
        Substitute.For<IIdempotencyRepository>();

    private readonly ILogger<FinalizeQuoteCommandHandler> _logger =
        Substitute.For<ILogger<FinalizeQuoteCommandHandler>>();

    private FinalizeQuoteCommandHandler BuildHandler() =>
        new(_b2bPersistenceService, _orderPersistenceService, _idempotencyRepository, _logger);

    private static B2BOrder BuildDraftOrderWithItem()
    {
        var order = B2BOrder.Start(
            CustomerId.CreateUnique(),
            "ACME Corp",
            Address.Create("Test St", "Prague", "CZ", "11000"));
        order.AddItem(ProductId.CreateUnique(), 2, Money.Create(100m, "USD"));
        order.ClearUncommittedEvents();
        return order;
    }

    private static FinalizeQuoteCommand ValidCommand(Guid b2bOrderId) =>
        new(b2bOrderId, "CreditCard", "idem-finalize-001");

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithOrderId_WhenQuoteIsFinalized()
    {
        var b2bOrder = BuildDraftOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;

        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _b2bPersistenceService
            .LoadB2BOrderAsync(b2bOrderId, Arg.Any<CancellationToken>())
            .Returns(b2bOrder);

        _b2bPersistenceService
            .UpdateB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<Func<B2BOrder, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(b2bOrderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        await _orderPersistenceService.Received(1).CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingOrderId_WhenDuplicateIdempotencyKey()
    {
        var existingOrderId = Guid.NewGuid();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyRecord("idem-finalize-dup", existingOrderId, DateTime.UtcNow));

        var result = await BuildHandler().Handle(
            new FinalizeQuoteCommand(Guid.NewGuid(), "CreditCard", "idem-finalize-dup"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingOrderId, result.Value);

        await _orderPersistenceService.DidNotReceive()
            .CreateOrderAsync(Arg.Any<Order>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenB2BOrderNotFound()
    {
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _b2bPersistenceService
            .LoadB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((B2BOrder?)null);

        var result = await BuildHandler().Handle(
            ValidCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderCreationThrows()
    {
        var b2bOrder = BuildDraftOrderWithItem();

        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _b2bPersistenceService
            .LoadB2BOrderAsync(b2bOrder.Id.Value, Arg.Any<CancellationToken>())
            .Returns(b2bOrder);

        _orderPersistenceService
            .CreateOrderAsync(Arg.Any<Order>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Payment gateway down"));

        var result = await BuildHandler().Handle(ValidCommand(b2bOrder.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Payment gateway down", result.Error);
    }
}

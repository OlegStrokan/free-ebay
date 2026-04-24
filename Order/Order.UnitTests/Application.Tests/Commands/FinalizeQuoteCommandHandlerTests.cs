using Application.Commands.FinalizeQuote;
using Application.Interfaces;
using Domain.Entities.B2BOrder;
using Domain.Entities.Order;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class FinalizeQuoteCommandHandlerTests
{
    private readonly IB2BOrderPersistenceService _b2bPersistenceService =
        Substitute.For<IB2BOrderPersistenceService>();

    private readonly ILogger<FinalizeQuoteCommandHandler> _logger =
        Substitute.For<ILogger<FinalizeQuoteCommandHandler>>();

    private FinalizeQuoteCommandHandler BuildHandler() =>
        new(_b2bPersistenceService, _logger);

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

        _b2bPersistenceService
            .LoadB2BOrderAsync(b2bOrderId, Arg.Any<CancellationToken>())
            .Returns(b2bOrder);

        _b2bPersistenceService
            .FinalizeB2BOrderAsync(
                b2bOrderId,
                Arg.Any<Order>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(b2bOrderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        await _b2bPersistenceService.Received(1).FinalizeB2BOrderAsync(
            b2bOrderId,
            Arg.Any<Order>(),
            "idem-finalize-001",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenB2BOrderNotFound()
    {
        _b2bPersistenceService
            .LoadB2BOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((B2BOrder?)null);

        var result = await BuildHandler().Handle(
            ValidCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);

        await _b2bPersistenceService.DidNotReceive().FinalizeB2BOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenFinalizeThrows()
    {
        var b2bOrder = BuildDraftOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;

        _b2bPersistenceService
            .LoadB2BOrderAsync(b2bOrderId, Arg.Any<CancellationToken>())
            .Returns(b2bOrder);

        _b2bPersistenceService
            .FinalizeB2BOrderAsync(
                Arg.Any<Guid>(),
                Arg.Any<Order>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Database transaction failed"));

        var result = await BuildHandler().Handle(ValidCommand(b2bOrderId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Database transaction failed", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldConvertB2BItemsToOrderItems_Correctly()
    {
        var customerId = CustomerId.CreateUnique();
        var productId1 = ProductId.CreateUnique();
        var productId2 = ProductId.CreateUnique();

        var b2bOrder = B2BOrder.Start(
            customerId,
            "Test Company",
            Address.Create("Street", "City", "Country", "12345"));
        b2bOrder.AddItem(productId1, 3, Money.Create(50m, "USD"));
        b2bOrder.AddItem(productId2, 2, Money.Create(75m, "USD"));
        b2bOrder.ClearUncommittedEvents();

        var b2bOrderId = b2bOrder.Id.Value;

        _b2bPersistenceService
            .LoadB2BOrderAsync(b2bOrderId, Arg.Any<CancellationToken>())
            .Returns(b2bOrder);

        Order? capturedOrder = null;
        _b2bPersistenceService
            .FinalizeB2BOrderAsync(
                b2bOrderId,
                Arg.Do<Order>(o => capturedOrder = o),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(b2bOrderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedOrder);
        Assert.Equal(2, capturedOrder.Items.Count());
        Assert.Contains(capturedOrder.Items, item => item.Quantity == 3);
        Assert.Contains(capturedOrder.Items, item => item.Quantity == 2);
    }
}

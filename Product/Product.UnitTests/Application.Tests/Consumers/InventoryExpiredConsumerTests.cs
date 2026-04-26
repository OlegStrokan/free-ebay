using System.Text.Json;
using Application.Commands.AdjustProductStock;
using Application.Common;
using Application.Consumers;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Consumers;

[TestFixture]
public class InventoryExpiredConsumerTests
{
    private ISender _sender = null!;
    private ILogger<InventoryExpiredConsumer> _logger = null!;
    private InventoryExpiredConsumer _consumer = null!;

    [SetUp]
    public void SetUp()
    {
        _sender = Substitute.For<ISender>();
        _logger = Substitute.For<ILogger<InventoryExpiredConsumer>>();
        _consumer = new InventoryExpiredConsumer(_sender, _logger);

        _sender.Send(Arg.Any<AdjustProductStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    private static JsonElement BuildPayload(string reservationId, (string productId, int qty)[] items)
    {
        var obj = new
        {
            reservationId,
            orderId = Guid.NewGuid().ToString(),
            status = "Expired",
            items = items.Select(i => new { productId = i.productId, quantity = i.qty }).ToArray(),
            occurredAtUtc = DateTime.UtcNow
        };
        return JsonSerializer.SerializeToElement(obj);
    }

    [Test]
    public void EventType_ShouldBeInventoryExpired()
    {
        Assert.That(_consumer.EventType, Is.EqualTo("InventoryExpired"));
    }

    [Test]
    public async Task ConsumeAsync_ShouldSendPositiveDeltaCommand_ForEachItem()
    {
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var payload = BuildPayload("res-1", [(productId1.ToString(), 5), (productId2.ToString(), 2)]);

        await _consumer.ConsumeAsync(payload, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AdjustProductStockCommand>(c => c.ProductId == productId1 && c.Delta == +5),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<AdjustProductStockCommand>(c => c.ProductId == productId2 && c.Delta == +2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldSkipItem_WhenProductIdIsInvalidGuid()
    {
        var payload = BuildPayload("res-1", [("not-a-valid-guid", 5)]);

        await _consumer.ConsumeAsync(payload, CancellationToken.None);

        await _sender.DidNotReceive().Send(Arg.Any<AdjustProductStockCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldDoNothing_WhenPayloadIsJsonNull()
    {
        var nullPayload = JsonSerializer.SerializeToElement<object?>(null);

        await _consumer.ConsumeAsync(nullPayload, CancellationToken.None);

        await _sender.DidNotReceive().Send(Arg.Any<AdjustProductStockCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumeAsync_ShouldContinueProcessingRemainingItems_WhenOneCommandFails()
    {
        var failingId = Guid.NewGuid();
        var goodId = Guid.NewGuid();
        var payload = BuildPayload("res-1", [(failingId.ToString(), 2), (goodId.ToString(), 4)]);

        _sender.Send(
                Arg.Is<AdjustProductStockCommand>(c => c.ProductId == failingId),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure("product not found"));

        await _consumer.ConsumeAsync(payload, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AdjustProductStockCommand>(c => c.ProductId == goodId && c.Delta == +4),
            Arg.Any<CancellationToken>());
    }
}

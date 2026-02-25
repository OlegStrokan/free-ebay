using System.Net;
using System.Text.Json;
using Application.DTOs;
using Application.Gateways.Exceptions;
using Infrastructure.Gateways;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Infrastructure.Tests.Gateways;

public class ShippingGatewayTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static ShippingGateway Build(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://shipping.test/")
        };
        var options = Options.Create(new ShippingApiOptions
        {
            BaseUrl    = "https://shipping.test/",
            ApiKey     = "test-key",
            TimeoutSeconds = 30
        });
        return new ShippingGateway(httpClient, options, Substitute.For<ILogger<ShippingGateway>>());
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(_respond(request));
    }

    private static StubHandler Respond(HttpStatusCode status, object? body = null)
    {
        var json = body is null ? "" : JsonSerializer.Serialize(body, JsonOpts);
        return new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }

    private static List<OrderItemDto> OneItem() =>
        new() { new OrderItemDto(Guid.NewGuid(), 1, 20m, "USD") };

    [Fact]
    public async Task CreateShipmentAsync_ShouldReturnShipmentResult_WhenSucceeds()
    {
        var handler = Respond(HttpStatusCode.OK, new
        {
            shipmentId     = "ship-1",
            trackingNumber = "TRK-001"
        });

        var result = await Build(handler).CreateShipmentAsync(
            Guid.NewGuid(),
            new AddressDto("Main St", "Prague", "CZ", "11000"),
            OneItem(),
            CancellationToken.None);

        Assert.Equal("ship-1",  result.ShipmentId);
        Assert.Equal("TRK-001", result.TrackingNumber);
    }

    [Fact]
    public async Task CreateShipmentAsync_ShouldThrowInvalidAddress_WhenServerReturnsBadRequest()
    {
        var handler = Respond(HttpStatusCode.BadRequest, new { error = "invalid address" });

        await Assert.ThrowsAsync<InvalidAddressException>(() =>
            Build(handler).CreateShipmentAsync(
                Guid.NewGuid(),
                new AddressDto("", "", "", ""),
                OneItem(),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateShipmentAsync_ShouldThrowInvalidAddress_WhenServerReturnsUnprocessableEntity()
    {
        var handler = Respond(HttpStatusCode.UnprocessableEntity, new { error = "postal code invalid" });

        await Assert.ThrowsAsync<InvalidAddressException>(() =>
            Build(handler).CreateShipmentAsync(
                Guid.NewGuid(),
                new AddressDto("St", "City", "CZ", "???"),
                OneItem(),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateShipmentAsync_ShouldThrowHttpRequestException_WhenServerReturnsInternalError()
    {
        var handler = Respond(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Build(handler).CreateShipmentAsync(
                Guid.NewGuid(),
                new AddressDto("St", "City", "CZ", "11000"),
                OneItem(),
                CancellationToken.None));
    }
    
    [Fact]
    public async Task CancelShipmentAsync_ShouldComplete_WhenSucceeds()
    {
        var handler = Respond(HttpStatusCode.OK);

        await Build(handler).CancelShipmentAsync("ship-1", CancellationToken.None);
    }

    [Fact]
    public async Task CancelShipmentAsync_ShouldNotThrow_WhenNotFound_IdempotentCancel()
    {
        // 404 = already canceled or unknown shipment - treated as idempotent success
        var handler = Respond(HttpStatusCode.NotFound);

        var ex = await Record.ExceptionAsync(() =>
            Build(handler).CancelShipmentAsync("ship-gone", CancellationToken.None));

        Assert.Null(ex);
    }
    
    [Fact]
    public async Task GetShipmentStatusAsync_ShouldReturnStatus_WhenFound()
    {
        var handler = Respond(HttpStatusCode.OK, new
        {
            trackingNumber        = "TRK-001",
            status                = "IN_TRANSIT",
            estimatedDeliveryDate = (DateTime?)null,
            actualDeliveryDate    = (DateTime?)null,
            currentLocation       = (string?)null
        });

        var result = await Build(handler).GetShipmentStatusAsync("TRK-001", CancellationToken.None);

        Assert.Equal("TRK-001",    result.TrackingNumber);
        Assert.Equal("IN_TRANSIT", result.Status);
    }

    [Fact]
    public async Task GetShipmentStatusAsync_ShouldThrowInvalidOperation_WhenNotFound()
    {
        var handler = Respond(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build(handler).GetShipmentStatusAsync("BAD-TRK", CancellationToken.None));
    }

    [Fact]
    public async Task CreateReturnShipmentAsync_ShouldReturnResult_WhenSucceeds()
    {
        var handler = Respond(HttpStatusCode.OK, new
        {
            returnShipmentId     = "ret-ship-1",
            returnTrackingNumber = "RET-TRK-001",
            expectedPickupDate   = DateTime.UtcNow.AddDays(2)
        });

        var result = await Build(handler).CreateReturnShipmentAsync(
            Guid.NewGuid(), Guid.NewGuid(), OneItem(), CancellationToken.None);

        Assert.Equal("ret-ship-1",   result.ReturnShipmentId);
        Assert.Equal("RET-TRK-001",  result.ReturnTrackingNumber);
    }

    [Fact]
    public async Task CreateReturnShipmentAsync_ShouldThrowInvalidAddress_WhenBadRequest()
    {
        var handler = Respond(HttpStatusCode.BadRequest, new { error = "invalid items" });

        await Assert.ThrowsAsync<InvalidAddressException>(() =>
            Build(handler).CreateReturnShipmentAsync(
                Guid.NewGuid(), Guid.NewGuid(), OneItem(), CancellationToken.None));
    }
}

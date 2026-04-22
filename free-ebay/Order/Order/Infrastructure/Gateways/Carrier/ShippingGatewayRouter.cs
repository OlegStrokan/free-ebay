using Application.Common.Enums;
using Application.DTOs;
using Application.DTOs.ShipmentGateway;
using Application.Gateways;

namespace Infrastructure.Gateways.Carrier;

public sealed class ShippingGatewayRouter(
    DpdShippingAdapter dpd,
    PplShippingAdapter ppl) : IShippingGateway
{
    public async Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        IReadOnlyCollection<OrderItemDto> items,
        ShippingCarrier carrier,
        CancellationToken cancellationToken)
    {
        var result = await SelectAdapter(carrier).CreateShipmentAsync(
            orderId, deliveryAddress, items, cancellationToken);

        // Prefix the returned IDs so the router can route subsequent calls
        return result with
        {
            ShipmentId = WrapId(carrier, result.ShipmentId),
            TrackingNumber = WrapId(carrier, result.TrackingNumber),
        };
    }

    public Task CancelShipmentAsync(string shipmentId, CancellationToken cancellationToken)
    {
        var (carrier, rawId) = UnwrapId(shipmentId);
        return SelectAdapter(carrier).CancelShipmentAsync(rawId, cancellationToken);
    }

    public Task<ShipmentStatusDto> GetShipmentStatusAsync(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        var (carrier, rawTracking) = UnwrapId(trackingNumber);
        return SelectAdapter(carrier).GetShipmentStatusAsync(rawTracking, cancellationToken);
    }

    public Task RegisterWebhookAsync(
        string shipmentId,
        string callbackUrl,
        string[] events,
        CancellationToken cancellationToken)
    {
        var (carrier, rawId) = UnwrapId(shipmentId);
        return SelectAdapter(carrier).RegisterWebhookAsync(rawId, callbackUrl, events, cancellationToken);
    }

    public async Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid orderId,
        Guid customerId,
        List<OrderItemDto> items,
        ShippingCarrier carrier,
        CancellationToken cancellationToken)
    {
        var result = await SelectAdapter(carrier).CreateReturnShipmentAsync(orderId, customerId, items, cancellationToken);

        return result with
        {
            ReturnShipmentId = WrapId(carrier, result.ReturnShipmentId),
            ReturnTrackingNumber = WrapId(carrier, result.ReturnTrackingNumber),
        };
    }

    public Task CancelReturnShipmentAsync(
        string returnShipmentId,
        string reason,
        CancellationToken cancellationToken)
    {
        var (carrier, rawId) = UnwrapId(returnShipmentId);
        return SelectAdapter(carrier).CancelReturnShipmentAsync(rawId, reason, cancellationToken);
    }

    public Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber,
        CancellationToken cancellationToken)
    {
        var (carrier, rawTracking) = UnwrapId(returnTrackingNumber);
        return SelectAdapter(carrier).GetReturnShipmentStatusAsync(rawTracking, cancellationToken);
    }

    private ICarrierAdapter SelectAdapter(ShippingCarrier carrier) => carrier switch
    {
        ShippingCarrier.Dpd => dpd,
        ShippingCarrier.Ppl => ppl,
        _ => throw new InvalidOperationException($"No adapter registered for carrier: {carrier}"),
    };

    private static string WrapId(ShippingCarrier carrier, string rawId) =>
        $"{carrier}:{rawId}";

    private static (ShippingCarrier carrier, string rawId) UnwrapId(string wrappedId)
    {
        var sep = wrappedId.IndexOf(':');
        if (sep < 0)
            throw new InvalidOperationException(
                $"Cannot determine carrier from ID '{wrappedId}'. Expected format: 'Carrier:RawId'.");

        var carrierStr = wrappedId[..sep];
        var rawId = wrappedId[(sep + 1)..];

        if (!Enum.TryParse<ShippingCarrier>(carrierStr, out var carrier))
            throw new InvalidOperationException(
                $"Unknown carrier prefix '{carrierStr}' in ID '{wrappedId}'.");

        return (carrier, rawId);
    }
}

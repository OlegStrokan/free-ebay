using System.Net;
using System.Text.Json;
using Application.DTOs;
using Application.DTOs.ShipmentGateway;
using Application.Gateways.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Gateways.Carrier;

public sealed class PplShippingAdapter : ICarrierAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<PplShippingAdapter> _logger;

    public PplShippingAdapter(
        HttpClient httpClient,
        IOptions<PplApiOptions> options,
        ILogger<PplShippingAdapter> logger)
    {
        _http = httpClient;
        _logger = logger;
        var opt = options.Value;

        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(opt.BaseUrl))
            _http.BaseAddress = new Uri(opt.BaseUrl);

        if (!string.IsNullOrWhiteSpace(opt.ApiKey))
        {
            _http.DefaultRequestHeaders.Remove("X-Api-Key");
            _http.DefaultRequestHeaders.Add("X-Api-Key", opt.ApiKey);
        }

        if (opt.TimeoutSeconds > 0)
            _http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
    }

    public async Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress,
        IReadOnlyCollection<OrderItemDto> items,
        CancellationToken cancellationToken)
    {
        var request = new CreateShipmentRequest(
            OrderId: orderId,
            Address: new AddressPayload(
                deliveryAddress.Street,
                deliveryAddress.City,
                deliveryAddress.Country,
                deliveryAddress.PostalCode),
            Packages: items.Select(i => new PackagePayload(i.ProductId, i.Quantity)).ToList()
        );

        using var response = await _http.PostAsJsonAsync(
            "api/v1/parcels",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new InvalidAddressException($"PPL rejected address/payload. Details: {err}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL create shipment failed. Status={(int)response.StatusCode}, Body={err}");
        }

        var dto = await response.Content.ReadFromJsonAsync<CreateShipmentResponse>(JsonOptions, cancellationToken)
                  ?? throw new InvalidOperationException("PPL create shipment response is empty.");

        _logger.LogInformation(
            "PPL shipment created. OrderId={OrderId}, ShipmentId={ShipmentId}, TrackingNumber={TrackingNumber}",
            orderId, dto.ShipmentId, dto.TrackingNumber);

        return new ShipmentResultDto(dto.ShipmentId, dto.TrackingNumber);
    }

    public async Task CancelShipmentAsync(string shipmentId, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(
            $"api/v1/parcels/{shipmentId}/cancel",
            content: null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("PPL CancelShipment: shipment not found (idempotent). ShipmentId={ShipmentId}", shipmentId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL cancel shipment failed. ShipmentId={shipmentId}, Status={(int)response.StatusCode}, Body={err}");
        }

        _logger.LogInformation("PPL shipment cancelled. ShipmentId={ShipmentId}", shipmentId);
    }

    public async Task<ShipmentStatusDto> GetShipmentStatusAsync(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(
            $"api/v1/parcels/tracking/{trackingNumber}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"PPL tracking number not found: {trackingNumber}");

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL get shipment status failed. TrackingNumber={trackingNumber}, Status={(int)response.StatusCode}, Body={err}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ShipmentStatusResponse>(JsonOptions, cancellationToken)
                          ?? throw new InvalidOperationException("PPL shipment status response is empty.");

        return new ShipmentStatusDto(
            TrackingNumber: apiResponse.TrackingNumber,
            Status: apiResponse.Status,
            EstimatedDeliveryDate: apiResponse.EstimatedDeliveryDate,
            ActualDeliveryDate: apiResponse.ActualDeliveryDate,
            CurrentLocation: apiResponse.CurrentLocation);
    }

    public async Task RegisterWebhookAsync(
        string shipmentId,
        string callbackUrl,
        string[] events,
        CancellationToken cancellationToken)
    {
        var request = new RegisterWebhookRequest(
            ShipmentId: shipmentId,
            CallbackUrl: callbackUrl,
            Events: events);

        using var response = await _http.PostAsJsonAsync(
            "api/v1/webhooks",
            request,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL webhook registration failed. ShipmentId={shipmentId}, Status={(int)response.StatusCode}, Body={err}");
        }

        _logger.LogInformation(
            "PPL webhook registered. ShipmentId={ShipmentId}, Events={Events}",
            shipmentId, string.Join(", ", events));
    }

    public async Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid orderId,
        Guid customerId,
        List<OrderItemDto> items,
        CancellationToken cancellationToken)
    {
        var request = new CreateReturnShipmentRequest(
            OrderId: orderId,
            CustomerId: customerId,
            Packages: items.Select(i => new PackagePayload(i.ProductId, i.Quantity)).ToList());

        using var response = await _http.PostAsJsonAsync(
            "api/v1/parcels/returns",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new InvalidAddressException($"PPL rejected return shipment. Details: {err}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL create return shipment failed. OrderId={orderId}, Status={(int)response.StatusCode}, Body={err}");
        }

        var dto = await response.Content.ReadFromJsonAsync<CreateReturnShipmentResponse>(JsonOptions, cancellationToken)
                  ?? throw new InvalidOperationException("PPL create return shipment response is empty.");

        _logger.LogInformation(
            "PPL return shipment created. OrderId={OrderId}, ReturnShipmentId={ReturnShipmentId}",
            orderId, dto.ReturnShipmentId);

        return new ReturnShipmentResultDto(dto.ReturnShipmentId, dto.ReturnTrackingNumber, dto.ExpectedPickupDate);
    }

    public async Task CancelReturnShipmentAsync(
        string returnShipmentId,
        string reason,
        CancellationToken cancellationToken)
    {
        var request = new CancelReturnShipmentRequest(Reason: reason);

        using var response = await _http.PostAsJsonAsync(
            $"api/v1/parcels/returns/{returnShipmentId}/cancel",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "PPL CancelReturnShipment: not found (idempotent). ReturnShipmentId={ReturnShipmentId}",
                returnShipmentId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL cancel return shipment failed. ReturnShipmentId={returnShipmentId}, Status={(int)response.StatusCode}, Body={err}");
        }

        _logger.LogInformation("PPL return shipment cancelled. ReturnShipmentId={ReturnShipmentId}", returnShipmentId);
    }

    public async Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber,
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(
            $"api/v1/parcels/returns/tracking/{returnTrackingNumber}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"PPL return tracking number not found: {returnTrackingNumber}");

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"PPL get return shipment status failed. ReturnTrackingNumber={returnTrackingNumber}, Status={(int)response.StatusCode}, Body={err}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ReturnShipmentStatusResponse>(JsonOptions, cancellationToken)
                          ?? throw new InvalidOperationException("PPL return shipment status response is empty.");

        return new ReturnShipmentStatusDto(
            ReturnTrackingNumber: apiResponse.ReturnTrackingNumber,
            Status: apiResponse.Status,
            PickedUpAt: apiResponse.PickedUpAt,
            DeliveredAt: apiResponse.DeliveredAt);
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return "<unreadable-body>"; }
    }

    // PPL-specific request/response shapes
    private sealed record CreateShipmentRequest(
        Guid OrderId,
        AddressPayload Address,
        IReadOnlyCollection<PackagePayload> Packages);

    private sealed record CreateShipmentResponse(string ShipmentId, string TrackingNumber);

    private sealed record ShipmentStatusResponse(
        string TrackingNumber,
        string Status,
        DateTime? EstimatedDeliveryDate,
        DateTime? ActualDeliveryDate,
        string? CurrentLocation);

    private sealed record RegisterWebhookRequest(string ShipmentId, string CallbackUrl, string[] Events);

    private sealed record CreateReturnShipmentRequest(
        Guid OrderId,
        Guid CustomerId,
        IReadOnlyCollection<PackagePayload> Packages);

    private sealed record CreateReturnShipmentResponse(
        string ReturnShipmentId,
        string ReturnTrackingNumber,
        DateTime ExpectedPickupDate);

    private sealed record ReturnShipmentStatusResponse(
        string ReturnTrackingNumber,
        string Status,
        DateTime? PickedUpAt,
        DateTime? DeliveredAt);

    private sealed record CancelReturnShipmentRequest(string Reason);

    private sealed record AddressPayload(string Street, string City, string Country, string PostalCode);
    private sealed record PackagePayload(Guid ProductId, int Quantity);
}

using System.Net;
using System.Text.Json;
using Application.DTOs;
using Application.DTOs.ShipmentGateway;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Gateways;

public sealed class ShippingGateway : IShippingGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly ShippingApiOptions options;
    private readonly ILogger<ShippingGateway> logger;

    public ShippingGateway(
        HttpClient httpClient,
        IOptions<ShippingApiOptions> options,
        ILogger<ShippingGateway> logger)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.logger = logger;

        if (httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(this.options.BaseUrl))
        {
            httpClient.BaseAddress = new Uri(this.options.BaseUrl);
        }

        if (!string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", this.options.ApiKey);
        }

        if (this.options.TimeoutSeconds > 0)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(this.options.TimeoutSeconds);
        }
    }

    public async Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto address,
        IReadOnlyCollection<OrderItemDto> items,
        CancellationToken cancellationToken)
    {
        var request = new CreateShipmentRequest(
            OrderId: orderId,
            Address: new AddressPayload(
                address.Street,
                address.City,
                address.Country,
                address.PostalCode),
            Items: items.Select(i => new ItemPayload(i.ProductId, i.Quantity)).ToList()
        );

        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/shipments",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new InvalidAddressException($"Shipping rejected address/payload. Details: {err}");
        }
        
        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<CreateShipmentResponse>(JsonOptions, cancellationToken)
                      ?? throw new InvalidOperationException("Shipping create response is empty.");

            logger.LogInformation(
                "Shipment created for OrderId={OrderId}. Shipment{ShipmentId}, TrackingNumber={TrackingNumber}",
                orderId,
                dto.ShipmentId,
                dto.TrackingNumber);

            return new ShipmentResultDto(dto.ShipmentId, dto.TrackingNumber);
        }

        var error = await SafeReadAsync(response, cancellationToken);
        throw new HttpRequestException(
            $"Shipping create failed. Status={(int)response.StatusCode}, Body={error}");
    }
    

    public async Task CancelShipmentAsync(string shipmentId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            $"api/v1/shipments/{shipmentId}/cancel",
            content: null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // @think: should we throw error?
            logger.LogWarning("CancelShipment: shipment not found. ShipmentId={ShipmentId}", shipmentId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Shipping cancel failed. ShipmentId={shipmentId}, Status={(int)response.StatusCode}, Body={err}");
        }

        logger.LogInformation("Shipment cancelled. ShipmentId={ShipmentId}", shipmentId);
    }
    
    public async Task<ShipmentStatusDto> GetShipmentStatusAsync(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"api/v1/shipments/tracking/{trackingNumber}",
            cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Tracking number not found: {trackingNumber}");

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Get shipment status failed. TrackingNumber={trackingNumber}," +
                $"Status={(int)response.StatusCode}, Body{err}");
        }

        var apiResponse =
            await response.Content.ReadFromJsonAsync<ShipmentStatusResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Shipment status response is empty.");

        return new ShipmentStatusDto(
            TrackingNumber: apiResponse.TrackingNumber,
            Status: apiResponse.Status,
            EstimatedDeliveryDate: apiResponse.EstimatedDeliveryDate,
            ActualDeliveryDate: apiResponse.ActualDeliveryDate,
            CurrentLocation: apiResponse.CurrentLocation);
    }

    public async Task RegisterWebhookAsync(string callbackUrl, CancellationToken cancellationToken)
    {
        var request = new RegisterWebhookRequest(callbackUrl);

        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/webhooks/register",
            request,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Webhook registration failed. CallbackUrl={callbackUrl}, Status={(int)response.StatusCode}, Body{err}");
        }

        logger.LogInformation("Webhook registered successfully. CallbackUrl={CallbackUrl}", callbackUrl);
    }

    public async Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid returnRequestId,
        Guid orderId,
        string originalTrackingNumber,
        AddressDto pickupAddress,
        CancellationToken cancellationToken)
    {
        var request = new CreateReturnShipmentRequest(
            ReturnRequestId: returnRequestId,
            OrderId: orderId,
            OriginalTrackingNumber: originalTrackingNumber,
            PickupAddress: new AddressPayload(
                pickupAddress.Street,
                pickupAddress.City,
                pickupAddress.Country,
                pickupAddress.PostalCode));

        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/shipment/returns",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new InvalidAddressException($"Return shipment rejected. Details: {err}");

        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Return shipment create failed. ReturnRequestId={returnRequestId}, Status{(int)response.StatusCode}, Body={err}");
        }

        var dto = await response.Content.ReadFromJsonAsync<CreateReturnShipmentResponse>(JsonOptions, cancellationToken)
                  ?? throw new InvalidOperationException("Return shipment create response is empty.");
        
        logger.LogInformation(
            "Return shipment created. ReturnRequestId={ReturnRequestId}, ReturnTrackingNumber={ReturnTrackingNumber}",
            returnRequestId,
            dto.ReturnTrackingNumber);

        return new ReturnShipmentResultDto(
            dto.ReturnShipmentId,
            dto.ReturnTrackingNumber,
            dto.ExpectedPickupDate);
    }

    public async Task CancelReturnShipmentAsync(string returnShipmentId, string reason, CancellationToken cancellationToken)
    {
        var request = new CancelReturnShipmentRequest(reason);

        using var response = await httpClient.PostAsJsonAsync(
            $"api/v1/shipments/returns/{returnShipmentId}/cancel",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("CancelReturnShipment: return shipment not found (treated as idempotent success). ReturnShipmentId={ReturnShipmentId}", returnShipmentId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Return shipment cancel failed. ReturnShipmentId={returnShipmentId}, Status={(int)response.StatusCode}, Body={err}");
        }

        logger.LogInformation("Return shipment cancelled. ReturnShipmentId={ReturnShipmentId}", returnShipmentId);
    }

    public async Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"api/v1/shipments/returns/tracking/{returnTrackingNumber}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Return tracking number not found: {returnTrackingNumber}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Get return shipment status failed. ReturnTrackingNumber={returnTrackingNumber}," +
                $"Status={(int)response.StatusCode}, Body={err}");
        }

        var apiResponse =
            await response.Content.ReadFromJsonAsync<ReturnShipmentStatusResponse>(JsonOptions, cancellationToken)
            ?? throw new HttpRequestException("Return shipment status response is empty.");

        return new ReturnShipmentStatusDto(
            ReturnTrackingNumber: apiResponse.ReturnTrackingNumber,
            Status: apiResponse.Status,
            PickedUpAt: apiResponse.PickedUpAt,
            DeliveredAt: apiResponse.DeliveredAt);
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            // @think: should we move it to the constant?
            return "<unreadable-body>";
        }
    }

    private sealed record CreateShipmentRequest(
        Guid OrderId,
        AddressPayload Address,
        IReadOnlyCollection<ItemPayload> Items);
    private sealed record CreateShipmentResponse(string ShipmentId, string TrackingNumber);

    private sealed record ShipmentStatusResponse(
        string TrackingNumber,
        string Status,
        DateTime? EstimatedDeliveryDate,
        DateTime? ActualDeliveryDate,
        string? CurrentLocation);

    private sealed record RegisterWebhookRequest(string CallbackUrl);
    private sealed record CancelReturnShipmentRequest(string Reason);

    private sealed record CreateReturnShipmentRequest(
        Guid ReturnRequestId,
        Guid OrderId,
        string OriginalTrackingNumber,
        AddressPayload PickupAddress);

    private sealed record CreateReturnShipmentResponse(
        string ReturnShipmentId,
        string ReturnTrackingNumber,
        DateTime ExpectedPickupDate);

    private sealed record ReturnShipmentStatusResponse(
        string ReturnTrackingNumber,
        string Status,
        DateTime? PickedUpAt,
        DateTime? DeliveredAt);
    
    private sealed record AddressPayload(string Street, string City, string Country, string PostalCode);
    private sealed record ItemPayload(Guid ProductId, int Quantity);
    
}
using System.Net;
using System.Text.Json;
using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Infrastructure.Gateways;

public class ShippingGateway : IShippingGateway
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
        List<OrderItemDto> items,
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
            throw new InvalidAddressException($"Shipping rejected address/payload. Details: {err}")
        }
        
        if (response.IsSuccessStatusCode)
        {
            var err = await SafeReadAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"Shipping create failed. Status={(int)response.StatusCode}, Body={err}");
        }

        var dto = await response.Content.ReadFromJsonAsync<CreateShipmentResponse>(JsonOptions, cancellationToken)
                  ?? throw new InvalidOperationException("Shipping create response is empty.");

        logger.LogInformation(
            "Shipment created for OrderId={OrderId}. Shipment{ShipmentId}, TrackingNumber={TrackingNumber}",
            orderId,
            dto.ShipmentId,
            dto.TrackingNumber);

        return new ShipmentResultDto(dto.ShipmentId, dto.TrackingNumber);
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
    private sealed record AddressPayload(string Street, string City, string Country, string PostalCode);
    private sealed record ItemPayload(Guid ProductId, int Quality);
    private sealed record CreateShipmentResponse(string ShipmentId, string TrackingNumber);
}
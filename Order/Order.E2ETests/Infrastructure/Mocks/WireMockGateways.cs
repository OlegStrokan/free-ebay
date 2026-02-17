using Application.DTOs;
using Application.DTOs.ShipmentGateway;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Grpc.Net.Client;
using Org.BouncyCastle.Bcpg;
using Protos.Accounting;
using Protos.Inventory;
using Protos.Payment;

namespace Order.E2ETests.Infrastructure.Mocks;

// these are the same gateway implementation of production code


//---------Payment Gateway---------//

public class FakeGrpcPaymentGateway : IPaymentGateway
{
    private readonly PaymentService.PaymentServiceClient _client;

    public FakeGrpcPaymentGateway(string serverAddress)
    {
        var channel = GrpcChannel.ForAddress(serverAddress);
        _client = new PaymentService.PaymentServiceClient(channel);
    }

    public async Task<string> ProcessPaymentAsync(
        Guid orderId, 
        Guid customerId,
        decimal amount,
        string currency, 
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        var response = await _client.ProcessPaymentAsync(
            new ProcessPaymentRequest
            {
                OrderId = orderId.ToString(),
                CustomerId = customerId.ToString(),
                Amount = (double)amount,
                Currency = currency,
                PaymentMethod = paymentMethod
            });

        if (!response.Success)
            throw new PaymentDeclinedException(
                $"[{response.ErrorCode}] {response.ErrorMessage}");

        return response.PaymentId;
    }

    public async Task<string> RefundAsync(
        string paymentId, 
        decimal amount,
        string reason, 
        CancellationToken cancellationToken
        )
    {
        var response = await _client.RefundPaymentAsync(
            new RefundPaymentRequest
            {
                PaymentId = paymentId,
                Amount = (double)amount,
                Reason = reason
            },
            cancellationToken: cancellationToken);

        if (!response.Success)
            throw new Exception($"Refund failed: {response.ErrorMessage}");

        return response.PaymentId;
    }
}

//---------Inventory Gateway---------//

public class FakeGrpcInventoryGateway : IInventoryGateway
{
    private readonly InventoryService.InventoryServiceClient _client;

    public FakeGrpcInventoryGateway(string serverAddress)
    {
        var channel = GrpcChannel.ForAddress(serverAddress);
        _client = new InventoryService.InventoryServiceClient(channel);
    }

    public async Task<string> ReserveAsync(
        Guid orderId,
        List<OrderItemDto> items,
        CancellationToken cancellationToken)
    {
        var request = new ReserveInventoryRequest
        {
            OrderId = orderId.ToString()
        };

        foreach (var item in items)
        {
            request.Items.Add(new InventoryItem
            {
                ProductId = item.ProductId.ToString(),
                Quality = item.Quantity
            });
        }

        var response = await _client.ReserveInventoryAsync(
            request,
            cancellationToken: cancellationToken);

        if (!response.Success)
            throw new InsufficientExecutionStackException(response.ErrorMessage);

        return response.ReservationId;
    }

    public async Task ReleaseReservationAsync(string reservationId, CancellationToken cancellationToken)
    {
        var response = await _client.ReleaseInventoryAsync(
            new ReleaseInventoryRequest { ReservationId = reservationId },
            cancellationToken: cancellationToken);

        if (!response.Success)
            throw new Exception($"Release failed: {response.ErrorMessage}");
    }
}

//---------Accounting Gateway---------//

public class FakeGrpcAccountingGateway : IAccountingGateway
{
    private readonly AccountingService.AccountingServiceClient _client;

    public FakeGrpcAccountingGateway(string serverAddress)
    {
        var channel = GrpcChannel.ForAddress(serverAddress);
        _client = new AccountingService.AccountingServiceClient(channel);
    }

    public async Task<string> RecordRefundAsync(
        Guid orderId, 
        string refundId, 
        decimal amount,
        string currency, 
        string reason,
        CancellationToken cancellationToken)
    {
        var response = await _client.RecordRefundAsync(
            new RecordRefundRequest
            {
                OrderId = orderId.ToString(),
                RefundId = refundId,
                Amount = (double)amount,
                Currency = currency,
                Reason = reason
            },
            cancellationToken: cancellationToken);

        if (!response.Success)
            throw new Exception($"RecordRefund failed: {response.ErrorMessage}");

        return response.TransactionId;
    }

    public async Task<string> ReverseRevenueAsync(
        Guid orderId,
        decimal amount,
        string currency, 
        List<OrderItemDto> returnedItems,
        CancellationToken cancellationToken)
    {
        var request = new ReverseRevenueRequest
        {
            OrderId = orderId.ToString(),
            Amount = (double)amount,
            Currency = currency,
        };

        foreach (var item in returnedItems)
        {
            request.ReturnedItems.Add(new AccountingItem
            {
                ProductId = item.ProductId.ToString(),
                Quantity = item.Quantity,
                Price = (double)item.Price,
                Currency = item.Currency
            });
        }

        var response = await _client.ReverseRevenueAsync(
            request,
            cancellationToken: cancellationToken);

        if (!response.Success)
            throw new Exception($"ReverseRevenue failed: {response.ErrorMessage}");

        return response.ReversalId;
    }

    public async Task CancelRevenueReversalAsync(string reversalId, string reason, CancellationToken cancellationToken)
    {
        // @todo
        // this method exists in th eproto but there is no rpc defined for it
        await Task.CompletedTask;
    }
}

//---------Shipping Gateway---------//

public class WireMockShippingGateway : IShippingGateway
{
    private readonly HttpClient _http;

    public WireMockShippingGateway(string wireMockBaseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };
    }

    public async Task<ShipmentResultDto> CreateShipmentAsync(
        Guid orderId,
        AddressDto deliveryAddress, 
        IReadOnlyCollection<OrderItemDto> items,
        CancellationToken cancellationToken)
    {
        var resp = await _http.PostAsJsonAsync(
            "/api/shipping/create",
            new { orderId, deliveryAddress, items },
            cancellationToken);

        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<ShipmentResponse>(cancellationToken)
                   ?? throw new Exception("Empty shipping response");

        return new ShipmentResultDto(body.ShipmentId, body.TrackingNumber);
    }

    public async Task CancelShipmentAsync(string shipmentId, CancellationToken cancellationToken)
    {
        var resp = await _http.PostAsJsonAsync(
            "/api/shipping/cancel",
            new { shipmentId },
            cancellationToken);

        resp.EnsureSuccessStatusCode();
    }

    // these shit is used by other flows (return saga ...) - throw for now in order e2e tests
    public Task<ShipmentStatusDto> GetShipmentStatusAsync(string trackingNumber, CancellationToken ct)
        => throw new NotImplementedException();

    public Task RegisterWebhookAsync(string callbackUrl, CancellationToken ct)
        => Task.CompletedTask;

    public Task<ReturnShipmentResultDto> CreateReturnShipmentAsync(
        Guid returnRequestId, Guid orderId, string originalTrackingNumber,
        AddressDto pickupAddress, CancellationToken ct)
        => throw new NotImplementedException();

    public Task<ReturnShipmentStatusDto> GetReturnShipmentStatusAsync(
        string returnTrackingNumber, CancellationToken ct)
        => throw new NotImplementedException();

    private record ShipmentResponse(string ShipmentId, string TrackingNumber);
}

using Domain.Common;
using FluentAssertions;
using Infrastructure.Extensions;
using Infrastructure.ReadModels;
using Order.E2ETests.Infrastructure;
using Protos.Order;
using Xunit;
using Xunit.Abstractions;
using Address = Protos.Order.Address;

namespace Order.E2ETests.Tests;

[Collection("E2E")]
public class B2BOrderE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private B2BOrderService.B2BOrderServiceClient _client = null!;

    public B2BOrderE2ETests(E2ETestServer server, ITestOutputHelper output)
    {
        _server = server;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _server.MigrateDatabaseAsync();
        _client = _server.CreateB2BOrderClient();
        _server.ResetAll();
    }

    public Task DisposeAsync() => Task.CompletedTask;


    [Fact]
    public async Task StartB2BOrder_HappyPath_ReadModelCreatedWithDraftStatus()
    {
        _output.WriteLine("B2B Happy Path - StartB2BOrder creates read model with Draft status");

        var customerId = Guid.NewGuid();
        var request = BuildStartRequest(customerId);

        var response = await _client.StartB2BOrderAsync(request);

        response.Success.Should().BeTrue("gRPC call must succeed");
        var b2bOrderId = Guid.Parse(response.B2BOrderId);
        _output.WriteLine($"B2BOrder created: {b2bOrderId}");

        var events = await _server.GetEventsAsync(b2bOrderId, AggregateTypes.B2BOrder);
        events.Should().ContainSingle(e => e.GetType().Name == "B2BOrderStartedEvent");
        _output.WriteLine($"Event store: {string.Join(", ", events.Select(e => e.GetType().Name))}");

        var readModel = await _server.WaitForB2BOrderReadModelStatusAsync(
            b2bOrderId, "Draft", timeoutSeconds: 20);

        readModel.Should().NotBeNull("read model must reach Draft within timeout");
        readModel!.Status.Should().Be("Draft");
        readModel.CustomerId.Should().Be(customerId);
        readModel.CompanyName.Should().Be(request.CompanyName);
        readModel.DeliveryStreet.Should().Be(request.DeliveryAddress.Street);
        readModel.DeliveryCity.Should().Be(request.DeliveryAddress.City);
        readModel.DeliveryCountry.Should().Be(request.DeliveryAddress.Country);
        readModel.DeliveryPostalCode.Should().Be(request.DeliveryAddress.PostalCode);

        _output.WriteLine($"ReadModel: Status={readModel.Status}, Company={readModel.CompanyName}");
        _output.WriteLine("PASSED: StartB2BOrder happy path");
    }

    [Fact]
    public async Task StartB2BOrder_Idempotency_DuplicateRequestReturnsSameId()
    {
        _output.WriteLine("B2B Idempotency - duplicate StartB2BOrder returns same B2BOrderId");

        var idempotencyKey = $"b2b-idem-{Guid.NewGuid()}";
        var request = BuildStartRequest(Guid.NewGuid(), idempotencyKey: idempotencyKey);

        _output.WriteLine("Request #1...");
        var response1 = await _client.StartB2BOrderAsync(request);

        _output.WriteLine("Request #2 (same idempotency key)...");
        var response2 = await _client.StartB2BOrderAsync(request);

        response1.Success.Should().BeTrue();
        response2.Success.Should().BeTrue();
        response1.B2BOrderId.Should().Be(response2.B2BOrderId,
            "same idempotency key must return the same B2BOrderId");

        _output.WriteLine($"Both returned: {response1.B2BOrderId}");

        var b2bOrderId = Guid.Parse(response1.B2BOrderId);
        var events = await _server.GetEventsAsync(b2bOrderId, AggregateTypes.B2BOrder);

        events.Count(e => e.GetType().Name == "B2BOrderStartedEvent")
            .Should().Be(1, "only ONE B2BOrderStartedEvent must exist in the event store");

        _output.WriteLine("PASSED: B2B idempotency works");
    }

    [Fact]
    public async Task UpdateQuoteDraft_AddItem_TotalPriceUpdatedInReadModel()
    {
        _output.WriteLine("UpdateQuoteDraft - AddItem updates TotalPrice in read model");

        var customerId = Guid.NewGuid();
        var startResp = await _client.StartB2BOrderAsync(BuildStartRequest(customerId));
        startResp.Success.Should().BeTrue();
        var b2bOrderId = Guid.Parse(startResp.B2BOrderId);

        var afterStart = await _server.WaitForB2BOrderReadModelStatusAsync(
            b2bOrderId, "Draft", timeoutSeconds: 20);
        afterStart.Should().NotBeNull("read model must appear before we continue");

        var productId = Guid.NewGuid().ToString();
        var updateResp = await _client.UpdateQuoteDraftAsync(new UpdateQuoteDraftRequest
        {
            B2BOrderId = b2bOrderId.ToString(),
            Changes =
            {
                new QuoteItemChange
                {
                    ChangeType = "AddItem",
                    ProductId = productId,
                    Quantity = 3,
                    Price = 50m.ToDecimalValue(),
                    Currency = "USD"
                }
            },
            Comment = "Added 3 units",
            CommentAuthor = "sales@acme.com"
        });

        updateResp.Success.Should().BeTrue();
        _output.WriteLine("UpdateQuoteDraft accepted");

        // wait for the read model to reflect the new total
        B2BOrderReadModel? updatedModel = null;
        for (var i = 0; i < 20; i++)
        {
            updatedModel = await _server.GetB2BOrderReadModelAsync(b2bOrderId);
            if (updatedModel?.TotalPrice > 0)
                break;
            await Task.Delay(1000);
        }

        updatedModel.Should().NotBeNull();
        updatedModel!.TotalPrice.Should().BeApproximately(150m, 0.01m,
            "3 items × $50 = $150");
        updatedModel.ItemsJson.Should().NotBeNullOrEmpty();
        updatedModel.CommentsJson.Should().Contain("Added 3 units");

        _output.WriteLine($"TotalPrice={updatedModel.TotalPrice}, Comment stored: {updatedModel.CommentsJson?.Length > 0}");
        _output.WriteLine("PASSED: UpdateQuoteDraft adds item and recalculates total");
    }

    // ─── Cancel ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelB2BOrder_ReadModelShouldShowCancelledStatus()
    {
        _output.WriteLine("CancelB2BOrder - read model transitions to Cancelled");

        var startResp = await _client.StartB2BOrderAsync(BuildStartRequest(Guid.NewGuid()));
        startResp.Success.Should().BeTrue();
        var b2bOrderId = Guid.Parse(startResp.B2BOrderId);

        var afterStart = await _server.WaitForB2BOrderReadModelStatusAsync(
            b2bOrderId, "Draft", timeoutSeconds: 20);
        afterStart.Should().NotBeNull();

        var cancelResp = await _client.CancelB2BOrderAsync(new CancelB2BOrderRequest
        {
            B2BOrderId = b2bOrderId.ToString(),
            Reasons = { "Customer withdrew the request" }
        });

        cancelResp.Success.Should().BeTrue();
        _output.WriteLine("CancelB2BOrder accepted");

        var readModel = await _server.WaitForB2BOrderReadModelStatusAsync(
            b2bOrderId, "Cancelled", timeoutSeconds: 20);

        readModel.Should().NotBeNull("read model must reach Cancelled within timeout");
        readModel!.Status.Should().Be("Cancelled");

        var events = await _server.GetEventsAsync(b2bOrderId, AggregateTypes.B2BOrder);
        events.Should().Contain(e => e.GetType().Name == "B2BOrderCancelledEvent");

        _output.WriteLine("PASSED: CancelB2BOrder transitions read model to Cancelled");
    }

    private static StartB2BOrderRequest BuildStartRequest(
        Guid customerId,
        string? idempotencyKey = null)
    {
        return new StartB2BOrderRequest
        {
            CustomerId = customerId.ToString(),
            CompanyName = "Acme Corp",
            IdempotencyKey = idempotencyKey ?? $"b2b-{Guid.NewGuid()}",
            DeliveryAddress = new Address
            {
                Street = "456 Industrial Blvd",
                City = "Chicago",
                Country = "US",
                PostalCode = "60601"
            }
        };
    }
}

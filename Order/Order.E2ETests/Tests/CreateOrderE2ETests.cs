using Application.Sagas;
using Confluent.Kafka;
using FluentAssertions;
using Order.E2ETests.Infrastructure;
using Org.BouncyCastle.Asn1.Ocsp;
using Protos.Order;
using WireMock.ResponseBuilders;
using Xunit;
using Xunit.Abstractions;
using Request = WireMock.RequestBuilders.Request;

namespace Order.E2ETests.Tests;

[Collection("E2E")]
public class CreateOrderE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private OrderService.OrderServiceClient _client = null!;


    public CreateOrderE2ETests(E2ETestServer testServer, ITestOutputHelper output)
    {
        _server = testServer;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _server.MigrateDatabaseAsync();
        _client = _server.CreateOrderClient();
        _server.ResetAll();
    }

    public Task DisposeAsync() => Task.CompletedTask;


    [Fact]
    public async Task CreateOrder_HappyPath_OrderCompletedSuccessfully()
    {
        _output.WriteLine("Happy Path - Full order flow");
        
        // arrange gprc fakes
        var paymentId = "paymentId";
        var reservationId = "reservationId";

        _server.PaymentService.ProcessShouldSucceed = true;
        _server.PaymentService.PaymentIdToReturn = paymentId;
        _server.InventoryService.ReserveShouldSucceed = true;
        _server.InventoryService.ReservationIdToReturn = reservationId;
        _server.Acccounting.ShouldSucceed = true;

        // arrange shipping wire mock - rest
        var shipmentId = "shipmentId";
        var trackingNumber = "trackingNumber";

        _server.Shipping
            .Given(Request.Create().WithPath("/api/shipping/create").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { shipmentId, trackingNumber }));
        var customerId = Guid.NewGuid();
        var request = BuildCreateOrderRequest(customerId);
        
        // act 
        var response = await _client.CreateOrderAsync(request);

        response.Success.Should().BeTrue("gRPC call must succeed");
        var orderId = Guid.Parse(response.OrderId);
        _output.WriteLine($"Order created: {orderId}");

        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // assert event store
        var events = await _server.GetEventsAsync(orderId, "Order");
        var eventTypes = events.Select(e => e.EventType).ToList();
        _output.WriteLine($"Events: {string.Join(", ", eventTypes)}");

        eventTypes.Should().Contain("OrderCreatedEvent");
        eventTypes.Should().Contain("OrderPaidEvent");
        eventTypes.Should().Contain("OrderTrackingAssignedEvent");
        eventTypes.Should().Contain("OrderCompletedEvent");
    
        // assert saga
        var saga = await _server.WaitForSagaStatusAsync(
            orderId, "OrderSaga", SagaStatus.Completed, timeoutSeconds: 30);

        saga.Should().NotBeNull("saga must reach Completed within timeout");
        saga!.Status.Should().Be(SagaStatus.Completed);
        _output.WriteLine($"Saga: {saga.Status}");

        // assert read model
        var readModel = await _server.WaitForReadModelStatusAsync(
            orderId, "Completed", timeoutSeconds: 30);

        readModel.Should().NotBeNull();
        readModel!.Status.Should().Be("Completed");
        readModel.CustomerId.Should().Be(customerId);
        readModel.TrackingId.Should().Be(trackingNumber);
        readModel.PaymentId.Should().Be(paymentId);
        _output.WriteLine($"ReadModel: Status={readModel.Status}, Payment={readModel.PaymentId}, Tracking={readModel.TrackingId}");

        // assert grpc calls
        _server.PaymentService.ProcessCalls.Should().HaveCount(1);
        _server.PaymentService.ProcessCalls[0].OrderId.Should().Be(orderId.ToString());
        _server.PaymentService.ProcessCalls[0].Currency.Should().Be("USD");
        _server.PaymentService.ProcessCalls[0].Amount.Should().BeApproximately(59.98, 0.01);

        _server.InventoryService.ReserveCalls.Should().HaveCount(1);
        _server.InventoryService.ReserveCalls[0].OrderId.Should().Be(orderId.ToString());
        _server.InventoryService.ReserveCalls[0].Items.Should().HaveCount(1);

        // assert shipping rest calls
        _server.Shipping.LogEntries.Should().ContainSingle(e => e.RequestMessage.Path = "/api/shipping/create");
        
        // assert email event on kafka. Email is not mocked - we only verify
        // OrderCompletedEvent reached Kafka. external system consumes from this same topic

        var emailEventOnKafka = await WasEventPublishedtoKafkaAsync(
            orderId, "OrderCompletedEvent");

        emailEventOnKafka.Should().BeTrue(
            "OrderCompletedEvent must be on Kafka so external email consumer can pick it up");

        _output.WriteLine("OrderCompletedEvent published to Kafka");
        
        _output.WriteLine("Passed: Full order flow end-to-end type shit");

    }

    [Fact]
    public async Task CreateOrder_Idempotency_DuplicateRequestSameOrderId()
    {
        _output.WriteLine("Idempotency - Duplicate request");

        _server.PaymentService.ProcessShouldSucceed = true;
        _server.InventoryService.ReserveShouldSucceed = true;

        _server.Shipping
            .Given(Request.Create().WithBodyAsJson("/api/shipping/create").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBodyAsJson(new { shipmentId = "ShipmentId", trackingNumber = "TrackingNumber " });
        
        // same key on both request - simulated client retry after network issue

        var idempotencyKey = $"idem-{Guid.NewGuid()}";
        var request = BuildCreateOrderRequest(Guid.NewGuid(), idempotencyKey: idempotencyKey);
        
        _output.WriteLine("Request #1...");
        var response1 = await _client.CreateOrderAsync(request);

        _output.WriteLine("Request #2 (same idempotency key)...");
        var response2 = await _client.CreateOrderAsync(request);

        response1.Success.Should().BeTrue();
        response2.Success.Should().BeTrue();
        response1.OrderId.Should().Be(response2.OrderId, "same key must return same order ID");
        
        _output.WriteLine($"Both returned: {response1.OrderId}");

        var orderId = Guid.Parse(response1.OrderId);
        var events = await _server.GetEventsAsync(orderId, "Order");

        events.Count(e => e.EventType == "OrderCreatedEvent")
            .Should().Be(1, "only ONE order must be created");

        _server.PaymentService.ProcessCalls.Should().HaveCount(1,
            "payment changed exactly once");
        
        _output.WriteLine("PASSED: Idempotency works!");
    }

    private static CreateOrderRequest BuildCreateOrderRequest(
        Guid customerId,
        string? idempotencyKey = null)
    {
        var req = new CreateOrderRequest
        {
            CustomerId = customerId.ToString(),
            PaymentMethod = "CreditCard",
            IdempotencyKey = idempotencyKey ?? $"test-{Guid.NewGuid()}",
            DeliveryAddress = new Address
            {
                Street = "123 Main Shit",
                City = "Kabul",
                Country = "Talibastan",
                PostalCode = "11092001"
            }
        };
        
        req.Items.Add(new OrderItem
        {
            ProductId = Guid.NewGuid().ToString(),
            Quantity = 2,
            Price = 29.99,
            Currency = "USD"
        });

        return req;
    }

    private async Task<bool> WasEventPublishedtoKafkaAsync(
        Guid orderId, string eventType, int timeoutSeconds = 15)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _server.KafkaBootstrapServers,
            GroupId = $"e2e-verify-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe("order.events");

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                var msg = consumer.Consume(TimeSpan.FromSeconds(1));
                if (msg?.Message?.Value is null) continue;

                if (msg.Message.Value.Contains(orderId.ToString()) &&
                    msg.Message.Value.Contains(eventType))
                    return true;
            }
        }

        finally
        {
            consumer.Unsubscribe();
        }

        return false;
    }
}
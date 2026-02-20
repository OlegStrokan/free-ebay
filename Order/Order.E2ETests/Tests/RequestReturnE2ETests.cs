using Application.Sagas;
using Application.Sagas.Persistence;
using Confluent.Kafka;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Order.E2ETests.Infrastructure;
using Protos.Order;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using Xunit.Abstractions;
using Address = Protos.Order.Address;
using OrderItem = Protos.Order.OrderItem;

namespace Order.E2ETests.Tests;

/// <summary>
/// E2E tests for Return Request flow.
/// 
/// Flow:
/// 1. RequestReturn gRPC → ReturnRequestCreatedEvent
/// 2. ReturnSaga starts: Validate → AwaitReturnShipment (webhook) → [PAUSE: WaitingForEvent]
/// 3. Webhook: ReturnShipmentDeliveredEvent → Resume saga
/// 4. ConfirmReturnReceived → ProcessRefund → UpdateAccounting → CompleteReturn
/// 
/// Tests:
/// - Happy path with webhook continuation
/// - Idempotency
/// - Compensation when refund fails
/// - Webhook timeout recovery via WatchdogService
/// </summary>
[Collection("E2E")]
public class RequestReturnE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private OrderService.OrderServiceClient _client = null!;

    public RequestReturnE2ETests(E2ETestServer server, ITestOutputHelper output)
    {
        _server = server;
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
    public async Task RequestReturn_HappyPath_WithWebhookContinuation_CompletesSuccessfully()
    {
        _output.WriteLine("🧪 Return Happy Path - Full flow with webhook continuation");

        var orderId = await CreateCompletedOrderAsync();
        _output.WriteLine($"✅ Setup: Order {orderId} completed");
        
        var returnShipmentId = "ReturnShipmentId";
        var returnTrackingNumber = "ReturnTrackingNumber";
        
        _server.ShipmentServer
            .Given(Request.Create().WithPath("/api/shipping/return/create").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new 
                { 
                    returnShipmentId, 
                    returnTrackingNumber,
                    labelUrl = "https://shipping.example.com/label/RSHIP-HAPPY-001.pdf"
                }));

        _server.ShipmentServer
            .Given(Request.Create().WithPath("/api/shipping/webhook/register").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var refundId = "RefundId";
        _server.PaymentService.RefundShouldSucceed = true;
        _server.PaymentService.RefundIdToReturn = refundId;

        _server.AccountingServer.ShouldSucceed = true;
        _server.AccountingServer.TransactionIdToReturn = "TransactionId";
        _server.AccountingServer.ReversalIdToReturn = "ReversalId";
        
        var returnRequest = new RequestReturnRequest
        {
            OrderId = orderId.ToString(),
            Reason = "Product defective - not as described",
            IdempotencyKey = $"return-{Guid.NewGuid()}"
        };

        returnRequest.ItemsToReturn.Add(new OrderItem
        {
            ProductId = Guid.NewGuid().ToString(),
            Quantity = 1,
            Price = 29.99,
            Currency = "USD"
        });

        _output.WriteLine("📤 Requesting return...");
        var returnResponse = await _client.RequestReturnAsync(returnRequest);

        returnResponse.Success.Should().BeTrue("gRPC call must succeed");
        // @think: shipment or requestId? 
        var returnRequestId = Guid.Parse(returnResponse.OrderId); // returns returnRequestId, not orderId
        _output.WriteLine($"✅ Return request accepted: {returnRequestId}");

        // Wait for: Outbox → Kafka → ReturnSaga starts
        await Task.Delay(TimeSpan.FromSeconds(5));

        // waiting for webhook 
        var saga = await _server.GetSagaStateAsync(orderId, "ReturnSaga");
        
        saga.Should().NotBeNull("saga must be created");
        saga!.Status.Should().Be(SagaStatus.WaitingForEvent,
            "saga should pause after AwaitReturnShipment step");
        saga.CurrentStep.Should().Be("AwaitReturnShipment");

        _output.WriteLine($"✅ Saga paused: Status={saga.Status}, Step={saga.CurrentStep}");

        _server.ShipmentServer.LogEntries.Should().ContainSingle(
            e => e.RequestMessage.Path == "/api/shipping/return/create");
        _server.ShipmentServer.LogEntries.Should().ContainSingle(
            e => e.RequestMessage.Path == "/api/shipping/webhook/register");

        // simulate webhook
        _output.WriteLine("📦 Simulating webhook: Return shipment delivered...");
        
        await PublishReturnShipmentDeliveredEventToKafkaAsync(
            orderId,
            returnShipmentId,
            returnTrackingNumber,
            deliveredAt: DateTime.UtcNow);

        // Wait for: Kafka → SagaContinuationEventHandler → Resume saga
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        saga = await _server.WaitForSagaStatusAsync(
            orderId, "ReturnSaga", SagaStatus.Completed, timeoutSeconds: 30);

        saga.Should().NotBeNull("saga must complete after webhook");
        saga!.Status.Should().Be(SagaStatus.Completed);

        _output.WriteLine($"✅ Saga completed: {saga.Status}");

        // all steps are completed
        var sagaRepo = _server.GetService<ISagaRepository>();
        var steps = await sagaRepo.GetStepLogsAsync(saga.Id, CancellationToken.None);

        var expectedSteps = new[]
        {
            "ValidateReturnRequest",
            "AwaitReturnShipment",
            "ConfirmReturnReceived",
            "ProcessRefund",
            "UpdateAccountingRecords",
            "CompleteReturn"
        };

        steps.Select(s => s.StepName).Should().BeEquivalentTo(expectedSteps);
        steps.Should().OnlyContain(s => s.Status == StepStatus.Completed);

        _output.WriteLine($"✅ All {steps.Count} steps completed");

        // check grpc servers 
        _server.PaymentService.RefundCalls.Should().HaveCount(1);
        _server.PaymentService.RefundCalls[0].Amount.Should().BeApproximately(29.99, 0.01);
        _server.PaymentService.RefundCalls[0].Reason.Should().Contain("Product defective");

        _server.AccountingServer.RecordRefundCalls.Should().HaveCount(1);
        _server.AccountingServer.RecordRefundCalls[0].RefundId.Should().Be(refundId);


        _server.AccountingServer.ReverseRevenueCalls.Should().HaveCount(1);
        _server.AccountingServer.ReverseRevenueCalls[0].Amount.Should().BeApproximately(29.99, 0.01);

        _output.WriteLine("✅ Payment & Accounting gRPC calls verified");

        // check if aggregate in correct state 
        var events = await _server.GetEventsAsync(returnRequestId, "ReturnRequest");
        var eventTypes = events.Select(e => e.EventType).ToList();

        eventTypes.Should().Contain("ReturnRequestCreatedEvent");
        eventTypes.Should().Contain("ReturnItemsReceivedEvent");
        eventTypes.Should().Contain("ReturnRefundProcessedEvent");
        eventTypes.Should().Contain("ReturnCompletedEvent");

        _output.WriteLine($"✅ ReturnRequest events: {string.Join(", ", eventTypes)}");

        // Rebuild aggregate from events
        var returnRequestAggregate = ReturnRequest.FromHistory(events);
        returnRequestAggregate.Status.Should().Be(ReturnStatus.Completed);
        returnRequestAggregate.RefundId.Should().Be(refundId);

        _output.WriteLine($"✅ ReturnRequest aggregate: Status={returnRequestAggregate.Status}");

        // check if read model is synced 
        var returnReadModel = await _server.WaitForReturnReadModelStatusAsync(
            returnRequestId, "Completed", timeoutSeconds: 30);

        returnReadModel.Should().NotBeNull();
        returnReadModel!.Status.Should().Be("Completed");
        returnReadModel.OrderId.Should().Be(orderId);
        returnReadModel.RefundAmount.Should().Be(29.99m);

        _output.WriteLine($"✅ Read model: Status={returnReadModel.Status}");

        _output.WriteLine("🎉 PASSED: Full return flow with webhook continuation!");
    }

    [Fact]
    public async Task ReqeustReturn_Idempotency_DuplicateRequestReturnsSameReturnRequestId()
    {
        _output.WriteLine("Return Idempotency - Duplicate request");
        
        var orderId = await CreateCompletedOrderAsync();
        
        _server.ShipmentServer
            .Given(Request.Create().WithPath("/api/shipping/return/create").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBodyAsJson(new { returnShipmentId = "returnShipmentId", returnTrackingNumber = "returnTrackingNumber" }));

        _server.ShipmentServer
            .Given(Request.Create().WithPath("/api/shipping/webhook/register").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
        
        // same idempotency key on both request
        var idempotencyKey = $"return-item-{Guid.NewGuid()}";
        
        var request = new RequestReturnRequest
        {
            OrderId = orderId.ToString(),
            Reason = "Change my mind",
            IdempotencyKey = idempotencyKey
        };
        
        request.ItemsToReturn.Add(new OrderItem
        {
            ProductId = Guid.NewGuid().ToString(),
            Quantity = 1,
            Price = 50.00,
            Currency = "USD"
        });
        
        _output.WriteLine("Request #1");
        var response1 = await _client.RequestReturnAsync(request);
        
        _output.WriteLine("Request #2");
        var response2 = await _client.RequestReturnAsync(request);

        response1.Success.Should().BeTrue();
        response2.Success.Should().BeTrue();
        response1.OrderId.Should().Be(response2.OrderId, "duplicate request must return same returnRequestId");
        
        _output.WriteLine("Both returned: {response1.OrderId}");

        var returnRequestId = Guid.Parse(response1.OrderId);
        var events = await _server.GetEventsAsync(returnRequestId, "ReturnRequest");

        events.Count(e => e.EventType == "ReturnRequestCreatedEvent")
            .Should().Be(1, "only ONE ReturnRequestCreatedEvent must exists");
        
        _output.WriteLine("PASSED: Idempotency work");
    }

    // starting point
    private async Task<Guid> CreateCompletedOrderAsync()
    {
        // Configure mocks for order creation
        _server.PaymentService.ProcessShouldSucceed = true;
        _server.PaymentService.PaymentIdToReturn = "PaymentId";
        
        _server.InventoryService.ReserveShouldSucceed = true;
        _server.InventoryService.ReservationIdToReturn = "ReservationId";

        _server.ShipmentServer
            .Given(Request.Create().WithPath("/api/shipping/create").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBodyAsJson(new { shipmentId = "ShipmentId", trackingNumber = "TrackingNumber" }));

        var customerId = Guid.NewGuid();
        var orderRequest = new CreateOrderRequest
        {
            CustomerId = customerId.ToString(),
            PaymentMethod = "CreditCard",
            IdempotencyKey = $"order-for-return-{Guid.NewGuid()}",
            DeliveryAddress = new Address
            {
                Street = "123 Main St",
                City = "New York",
                Country = "USA",
                PostalCode = "10001"
            }
        };

        orderRequest.Items.Add(new OrderItem
        {
            ProductId = Guid.NewGuid().ToString(),
            Quantity = 1,
            Price = 29.99,
            Currency = "USD"
        });

        var orderResponse = await _client.CreateOrderAsync(orderRequest);
        var orderId = Guid.Parse(orderResponse.OrderId);

        // Wait for order saga to complete
        await _server.WaitForSagaStatusAsync(
            orderId, "OrderSaga", SagaStatus.Completed, timeoutSeconds: 30);

        return orderId;
    }


    // publishes event to Kafka to simulate webhook, triggers continuation to resume the paused ReturnSaga
    private async Task PublishReturnShipmentDeliveredEventToKafkaAsync(
        Guid orderId,
        string shipmentId,
        string trackingNumber,
        DateTime deliveredAt)
    {
        var eventWrapper = new
        {
            EventType = "ReturnShipmentDeliveredEvent",
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                OrderId = orderId,
                ShipmentId = shipmentId,
                TrackingNumber = trackingNumber,
                DeliveredAt = deliveredAt
            })
        };

        var config = new ProducerConfig
        {
            BootstrapServers = _server.KafkaBootstrapServers
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();
        
        await producer.ProduceAsync(
            "order.events",
            new Message<string, string>
            {
                Key = orderId.ToString(),
                Value = System.Text.Json.JsonSerializer.Serialize(eventWrapper)
            });

        _output.WriteLine($"📤 Published ReturnShipmentDeliveredEvent to Kafka for order {orderId}");
    }
}
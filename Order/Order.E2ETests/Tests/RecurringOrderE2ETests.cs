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
public class RecurringOrderE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private RecurringOrderService.RecurringOrderServiceClient _client = null!;

    public RecurringOrderE2ETests(E2ETestServer server, ITestOutputHelper output)
    {
        _server = server;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _server.MigrateDatabaseAsync();
        _client = _server.CreateRecurringOrderClient();
        _server.ResetAll();
    }

    public Task DisposeAsync() => Task.CompletedTask;
    
    [Fact]
    public async Task CreateRecurringOrder_HappyPath_ReadModelCreatedWithActiveStatus()
    {
        _output.WriteLine("RecurringOrder Happy Path - CreateRecurringOrder creates read model with Active status");

        var customerId = Guid.NewGuid();
        var request = BuildCreateRequest(customerId);

        var response = await _client.CreateRecurringOrderAsync(request);

        response.Success.Should().BeTrue("gRPC call must succeed");
        var recurringOrderId = Guid.Parse(response.RecurringOrderId);
        _output.WriteLine($"RecurringOrder created: {recurringOrderId}");

        var events = await _server.GetEventsAsync(recurringOrderId, AggregateTypes.RecurringOrder);
        events.Should().ContainSingle(e => e.GetType().Name == "RecurringOrderCreatedEvent");
        _output.WriteLine($"Event store: {string.Join(", ", events.Select(e => e.GetType().Name))}");

        var readModel = await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Active", timeoutSeconds: 20);

        readModel.Should().NotBeNull("read model must reach Active within timeout");
        readModel!.Status.Should().Be("Active");
        readModel.CustomerId.Should().Be(customerId);
        readModel.PaymentMethod.Should().Be(request.PaymentMethod);
        readModel.Frequency.Should().Be(request.Frequency);
        readModel.DeliveryStreet.Should().Be(request.DeliveryAddress.Street);
        readModel.DeliveryCity.Should().Be(request.DeliveryAddress.City);
        readModel.DeliveryCountry.Should().Be(request.DeliveryAddress.Country);
        readModel.DeliveryPostalCode.Should().Be(request.DeliveryAddress.PostalCode);
        readModel.TotalExecutions.Should().Be(0);

        _output.WriteLine($"ReadModel: Status={readModel.Status}, Frequency={readModel.Frequency}");
        _output.WriteLine("PASSED: CreateRecurringOrder happy path");
    }

    [Fact]
    public async Task CreateRecurringOrder_Idempotency_DuplicateRequestReturnsSameId()
    {
        _output.WriteLine("RecurringOrder Idempotency - duplicate CreateRecurringOrder returns same RecurringOrderId");

        var idempotencyKey = $"recurring-idem-{Guid.NewGuid()}";
        var request = BuildCreateRequest(Guid.NewGuid(), idempotencyKey: idempotencyKey);

        _output.WriteLine("Request #1...");
        var response1 = await _client.CreateRecurringOrderAsync(request);

        _output.WriteLine("Request #2 (same idempotency key)...");
        var response2 = await _client.CreateRecurringOrderAsync(request);

        response1.Success.Should().BeTrue();
        response2.Success.Should().BeTrue();
        response1.RecurringOrderId.Should().Be(response2.RecurringOrderId,
            "same idempotency key must return the same RecurringOrderId");

        _output.WriteLine($"Both returned: {response1.RecurringOrderId}");

        var recurringOrderId = Guid.Parse(response1.RecurringOrderId);
        var events = await _server.GetEventsAsync(recurringOrderId, AggregateTypes.RecurringOrder);

        events.Count(e => e.GetType().Name == "RecurringOrderCreatedEvent")
            .Should().Be(1, "only ONE RecurringOrderCreatedEvent must exist in the event store");

        _output.WriteLine("PASSED: RecurringOrder idempotency works");
    }
    
    [Fact]
    public async Task PauseRecurringOrder_HappyPath_ReadModelChangesToPaused()
    {
        _output.WriteLine("PauseRecurringOrder - read model transitions to Paused");

        var createResp = await _client.CreateRecurringOrderAsync(BuildCreateRequest(Guid.NewGuid()));
        createResp.Success.Should().BeTrue();
        var recurringOrderId = Guid.Parse(createResp.RecurringOrderId);

        var afterCreate = await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Active", timeoutSeconds: 20);
        afterCreate.Should().NotBeNull("read model must appear before we continue");

        var pauseResp = await _client.PauseRecurringOrderAsync(new PauseRecurringOrderRequest
        {
            RecurringOrderId = recurringOrderId.ToString()
        });

        pauseResp.Success.Should().BeTrue();
        _output.WriteLine("PauseRecurringOrder accepted");

        var readModel = await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Paused", timeoutSeconds: 20);

        readModel.Should().NotBeNull("read model must reach Paused within timeout");
        readModel!.Status.Should().Be("Paused");

        var events = await _server.GetEventsAsync(recurringOrderId, AggregateTypes.RecurringOrder);
        events.Should().Contain(e => e.GetType().Name == "RecurringOrderPausedEvent");

        _output.WriteLine("PASSED: PauseRecurringOrder transitions read model to Paused");
    }
    
    [Fact]
    public async Task ResumeRecurringOrder_HappyPath_ReadModelChangesBackToActive()
    {
        _output.WriteLine("ResumeRecurringOrder - read model transitions back to Active");

        var createResp = await _client.CreateRecurringOrderAsync(BuildCreateRequest(Guid.NewGuid()));
        createResp.Success.Should().BeTrue();
        var recurringOrderId = Guid.Parse(createResp.RecurringOrderId);

        await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Active", timeoutSeconds: 20);

        var pauseResp = await _client.PauseRecurringOrderAsync(new PauseRecurringOrderRequest
        {
            RecurringOrderId = recurringOrderId.ToString()
        });
        pauseResp.Success.Should().BeTrue();

        await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Paused", timeoutSeconds: 20);

        var resumeResp = await _client.ResumeRecurringOrderAsync(new ResumeRecurringOrderRequest
        {
            RecurringOrderId = recurringOrderId.ToString()
        });

        resumeResp.Success.Should().BeTrue();
        _output.WriteLine("ResumeRecurringOrder accepted");

        var readModel = await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Active", timeoutSeconds: 20);

        readModel.Should().NotBeNull("read model must return to Active within timeout");
        readModel!.Status.Should().Be("Active");

        var events = await _server.GetEventsAsync(recurringOrderId, AggregateTypes.RecurringOrder);
        events.Should().Contain(e => e.GetType().Name == "RecurringOrderResumedEvent");

        _output.WriteLine("PASSED: ResumeRecurringOrder transitions read model back to Active");
    }
    
    [Fact]
    public async Task CancelRecurringOrder_HappyPath_ReadModelChangesToCancelled()
    {
        _output.WriteLine("CancelRecurringOrder - read model transitions to Cancelled");

        var createResp = await _client.CreateRecurringOrderAsync(BuildCreateRequest(Guid.NewGuid()));
        createResp.Success.Should().BeTrue();
        var recurringOrderId = Guid.Parse(createResp.RecurringOrderId);

        var afterCreate = await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Active", timeoutSeconds: 20);
        afterCreate.Should().NotBeNull();

        var cancelResp = await _client.CancelRecurringOrderAsync(new CancelRecurringOrderRequest
        {
            RecurringOrderId = recurringOrderId.ToString(),
            Reason = "Customer requested cancellation"
        });

        cancelResp.Success.Should().BeTrue();
        _output.WriteLine("CancelRecurringOrder accepted");

        var readModel = await _server.WaitForRecurringOrderReadModelStatusAsync(
            recurringOrderId, "Cancelled", timeoutSeconds: 20);

        readModel.Should().NotBeNull("read model must reach Cancelled within timeout");
        readModel!.Status.Should().Be("Cancelled");

        var events = await _server.GetEventsAsync(recurringOrderId, AggregateTypes.RecurringOrder);
        events.Should().Contain(e => e.GetType().Name == "RecurringOrderCancelledEvent");

        _output.WriteLine("PASSED: CancelRecurringOrder transitions read model to Cancelled");
    }
    
    private static CreateRecurringOrderRequest BuildCreateRequest(
        Guid customerId,
        string? idempotencyKey = null)
    {
        var request = new CreateRecurringOrderRequest
        {
            CustomerId = customerId.ToString(),
            PaymentMethod = "Card-123",
            Frequency = "Weekly",
            DeliveryAddress = new Address
            {
                Street = "Wenceslas Sq 1",
                City = "Prague",
                Country = "CZ",
                PostalCode = "11000"
            },
            FirstRunAt = "",
            MaxExecutions = 0,
            IdempotencyKey = idempotencyKey ?? $"recurring-{Guid.NewGuid()}"
        };
        request.Items.Add(new RecurringItem
        {
            ProductId = Guid.NewGuid().ToString(),
            Quantity = 2,
            Price = 50m.ToDecimalValue(),
            Currency = "USD"
        });
        return request;
    }
}

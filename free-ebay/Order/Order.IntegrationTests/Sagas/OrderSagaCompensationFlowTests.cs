using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Application.Sagas;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Application.Sagas.Persistence;
using Application.Sagas.Steps;
using Domain.Entities.Order;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Order.IntegrationTests.Infrastructure;
using Xunit;
using OrderAggregate = Domain.Entities.Order.Order;

namespace Order.IntegrationTests.Sagas;

[Collection("Integration")]
public sealed class OrderSagaCompensationFlowTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public OrderSagaCompensationFlowTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExecuteAsync_ShouldCompensateInReverseOrder_AndCancelOrder_WithRealSagaRepository()
    {
        await using var scope = _fixture.CreateScope();

        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
        var realOrderPersistence = scope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();

        var (order, data) = BuildPendingOrderAndData();
        await realOrderPersistence.CreateOrderAsync(order, $"idem-{Guid.NewGuid()}", CancellationToken.None);

        var compensationSequence = new List<string>();
        var orderPersistence = new RecordingOrderPersistenceService(realOrderPersistence, compensationSequence);

        var steps = new ISagaStep<OrderSagaData, OrderSagaContext>[]
        {
            new CancelOrderOnFailureStep(
                orderPersistence,
                new NoopIncidentReporter(),
                NullLogger<CancelOrderOnFailureStep>.Instance),
            new RecordingSuccessfulStep("ReserveInventory", 1, compensationSequence),
            new RecordingSuccessfulStep("CreateShipment", 2, compensationSequence),
            new FailingStep("ProcessPayment", 3, "Payment was declined")
        };

        var saga = new Application.Sagas.OrderSaga.OrderSaga(
            sagaRepository,
            steps,
            new NonTransientErrorClassifier(),
            NullLogger<Application.Sagas.OrderSaga.OrderSaga>.Instance);

        var result = await saga.ExecuteAsync(data, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(SagaStatus.Failed);

        compensationSequence.Should().Equal(
            "CreateShipment.Compensate",
            "ReserveInventory.Compensate",
            "CancelOrderOnFailure.Compensate");

        var persistedSaga = await sagaRepository.GetByCorrelationIdAsync(
            data.CorrelationId,
            "OrderSaga",
            CancellationToken.None);

        persistedSaga.Should().NotBeNull();
        persistedSaga!.Status.Should().Be(SagaStatus.Compensated);

        var stepLogs = await sagaRepository.GetStepLogsAsync(persistedSaga.Id, CancellationToken.None);

        stepLogs.Single(s => s.StepName == "CreateShipment").Status.Should().Be(StepStatus.Compensated);
        stepLogs.Single(s => s.StepName == "ReserveInventory").Status.Should().Be(StepStatus.Compensated);
        stepLogs.Single(s => s.StepName == "ProcessPayment").Status.Should().Be(StepStatus.Failed);
        stepLogs.Single(s => s.StepName == "CancelOrderOnFailure").Status.Should().Be(StepStatus.Compensated);

        var reloadedOrder = await realOrderPersistence.LoadOrderAsync(data.CorrelationId, CancellationToken.None);
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.Status.Should().Be(OrderStatus.Cancelled);
    }

    private static (OrderAggregate Order, OrderSagaData Data) BuildPendingOrderAndData()
    {
        var customerId = CustomerId.CreateUnique();

        var order = OrderAggregate.Create(
            customerId,
            Address.Create("Baker St", "London", "UK", "NW1"),
            new List<OrderItem>
            {
                OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(25m, "USD"))
            });

        var data = new OrderSagaData
        {
            CorrelationId = order.Id.Value,
            CustomerId = customerId.Value,
            TotalAmount = 25m,
            Currency = "USD",
            PaymentMethod = "CreditCard",
            DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
            Items =
            [
                new OrderItemDto(Guid.NewGuid(), 1, 25m, "USD")
            ]
        };

        return (order, data);
    }

    private sealed class RecordingSuccessfulStep(
        string stepName,
        int order,
        List<string> compensationSequence)
        : ISagaStep<OrderSagaData, OrderSagaContext>
    {
        public string StepName => stepName;
        public int Order => order;

        public Task<StepResult> ExecuteAsync(
            OrderSagaData data,
            OrderSagaContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(StepResult.SuccessResult());

        public Task CompensateAsync(
            OrderSagaData data,
            OrderSagaContext context,
            CancellationToken cancellationToken)
        {
            compensationSequence.Add($"{StepName}.Compensate");
            return Task.CompletedTask;
        }
    }

    private sealed class FailingStep(string stepName, int order, string error)
        : ISagaStep<OrderSagaData, OrderSagaContext>
    {
        public string StepName => stepName;
        public int Order => order;

        public Task<StepResult> ExecuteAsync(
            OrderSagaData data,
            OrderSagaContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(StepResult.Failure(error));

        public Task CompensateAsync(
            OrderSagaData data,
            OrderSagaContext context,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class RecordingOrderPersistenceService(
        IOrderPersistenceService inner,
        List<string> compensationSequence)
        : IOrderPersistenceService
    {
        public Task CreateOrderAsync(
            OrderAggregate order,
            string idempotencyKey,
            CancellationToken cancellationToken)
            => inner.CreateOrderAsync(order, idempotencyKey, cancellationToken);

        public async Task UpdateOrderAsync(
            Guid orderId,
            Func<OrderAggregate, Task> action,
            CancellationToken ct)
        {
            compensationSequence.Add("CancelOrderOnFailure.Compensate");
            await inner.UpdateOrderAsync(orderId, action, ct);
        }

        public Task<OrderAggregate?> LoadOrderAsync(Guid orderId, CancellationToken ct)
            => inner.LoadOrderAsync(orderId, ct);
    }

    private sealed class NonTransientErrorClassifier : ISagaErrorClassifier
    {
        public bool IsTransient(Exception ex) => false;
    }

    private sealed class NoopIncidentReporter : IIncidentReporter
    {
        public Task SendAlertAsync(IncidentAlert alert, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task CreateInterventionTicketAsync(InterventionTicket ticket, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
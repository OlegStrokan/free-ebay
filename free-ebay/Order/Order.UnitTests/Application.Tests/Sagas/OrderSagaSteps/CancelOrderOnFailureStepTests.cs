using Application.Common.Enums;
using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Domain.Entities.Order;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class CancelOrderOnFailureStepTests
{
    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly IIncidentReporter _incidentReporter =
        Substitute.For<IIncidentReporter>();

    private readonly ILogger<CancelOrderOnFailureStep> _logger =
        Substitute.For<ILogger<CancelOrderOnFailureStep>>();

    private CancelOrderOnFailureStep BuildStep() =>
        new(_orderPersistenceService, _incidentReporter, _logger);

    [Fact]
    public async Task CompensateAsync_ShouldCancelOrder_WhenOrderCanTransitionToCancelled()
    {
        var (order, data) = BuildOrderAndData(OrderStatus.Pending);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        _orderPersistenceService
            .UpdateOrderAsync(data.CorrelationId, Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var action = ci.ArgAt<Func<Order, Task>>(1);
                return action(order);
            });

        await BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, order.Status);

        await _orderPersistenceService.Received(1).UpdateOrderAsync(
            data.CorrelationId,
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.DidNotReceive().SendAlertAsync(
            Arg.Any<IncidentAlert>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.DidNotReceive().CreateInterventionTicketAsync(
            Arg.Any<InterventionTicket>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNoOp_WhenOrderAlreadyCancelled()
    {
        var (order, data) = BuildOrderAndData(OrderStatus.Cancelled);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        await BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _orderPersistenceService.DidNotReceive().UpdateOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.DidNotReceive().SendAlertAsync(
            Arg.Any<IncidentAlert>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.DidNotReceive().CreateInterventionTicketAsync(
            Arg.Any<InterventionTicket>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldRaiseCriticalIncident_WhenOrderAlreadyCompleted()
    {
        var (order, data) = BuildOrderAndData(OrderStatus.Completed);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        await BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _orderPersistenceService.DidNotReceive().UpdateOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.Received(1).SendAlertAsync(
            Arg.Is<IncidentAlert>(a =>
                a.OrderId == data.CorrelationId &&
                a.Severity == AlertSeverity.Critical &&
                a.Message.Contains("already Completed")),
            Arg.Any<CancellationToken>());

        await _incidentReporter.Received(1).CreateInterventionTicketAsync(
            Arg.Is<InterventionTicket>(t =>
                t.OrderId == data.CorrelationId &&
                t.Issue.Contains("already Completed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldRaiseCriticalIncident_WhenOrderCannotTransitionToCancelled()
    {
        var (order, data) = BuildOrderAndData(OrderStatus.Approved);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        await BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _orderPersistenceService.DidNotReceive().UpdateOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.Received(1).SendAlertAsync(
            Arg.Is<IncidentAlert>(a =>
                a.OrderId == data.CorrelationId &&
                a.Severity == AlertSeverity.Critical &&
                a.Message.Contains("cannot transition")),
            Arg.Any<CancellationToken>());

        await _incidentReporter.Received(1).CreateInterventionTicketAsync(
            Arg.Is<InterventionTicket>(t =>
                t.OrderId == data.CorrelationId &&
                t.Issue.Contains("cannot transition")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldRaiseCriticalIncident_WhenCancellationFails()
    {
        var (order, data) = BuildOrderAndData(OrderStatus.Pending);

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        _orderPersistenceService
            .UpdateOrderAsync(data.CorrelationId, Arg.Any<Func<Order, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database timeout"));

        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None));

        Assert.Null(exception);

        await _incidentReporter.Received(1).SendAlertAsync(
            Arg.Is<IncidentAlert>(a =>
                a.OrderId == data.CorrelationId &&
                a.Severity == AlertSeverity.Critical &&
                a.Message.Contains("Database timeout")),
            Arg.Any<CancellationToken>());

        await _incidentReporter.Received(1).CreateInterventionTicketAsync(
            Arg.Is<InterventionTicket>(t =>
                t.OrderId == data.CorrelationId &&
                t.Issue.Contains("Database timeout")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNoOp_WhenOrderDoesNotExist()
    {
        var data = CreateSampleData(Guid.NewGuid(), Guid.NewGuid());

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        await BuildStep().CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _orderPersistenceService.DidNotReceive().UpdateOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<Order, Task>>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.DidNotReceive().SendAlertAsync(
            Arg.Any<IncidentAlert>(),
            Arg.Any<CancellationToken>());

        await _incidentReporter.DidNotReceive().CreateInterventionTicketAsync(
            Arg.Any<InterventionTicket>(),
            Arg.Any<CancellationToken>());
    }

    private static (Order Order, OrderSagaData Data) BuildOrderAndData(OrderStatus status)
    {
        var customerId = CustomerId.From(Guid.NewGuid());

        var order = Order.Create(
            customerId,
            Address.Create("Baker St", "London", "UK", "NW1"),
            new List<OrderItem>
            {
                OrderItem.Create(ProductId.From(Guid.NewGuid()), 1, Money.Create(100m, "USD"))
            });

        if (status == OrderStatus.Paid || status == OrderStatus.Approved || status == OrderStatus.Completed)
        {
            order.Pay(PaymentId.From("PAY-123"));
        }

        if (status == OrderStatus.Approved || status == OrderStatus.Completed)
        {
            order.Approve();
        }

        if (status == OrderStatus.Completed)
        {
            order.Complete();
        }

        if (status == OrderStatus.Cancelled)
        {
            order.Cancel(new List<string> { "Initial cancelled state" });
        }

        var data = CreateSampleData(order.Id.Value, customerId.Value);
        return (order, data);
    }

    private static OrderSagaData CreateSampleData(Guid orderId, Guid customerId) =>
        new()
        {
            CorrelationId = orderId,
            CustomerId = customerId,
            PaymentMethod = "CreditCard",
            TotalAmount = 100m,
            Currency = "USD",
            DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
            Items = new List<OrderItemDto>
            {
                new(Guid.NewGuid(), 1, 100m, "USD")
            }
        };
}
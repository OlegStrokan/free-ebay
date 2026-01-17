using Application.Commands.CreateOrder;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();
    private readonly ILogger<CreateOrderCommandHandler> _logger = Substitute.For<ILogger<CreateOrderCommandHandler>>();

    public CreateOrderCommandHandlerTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);
    }
    
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOrderIsCreated()
    {
        var handler = new CreateOrderCommandHandler(_orderRepository, _outboxRepository, _unitOfWork, _logger);
        var command = CreateValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);
        
        Assert.True(result.IsSuccess);

        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        
        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            nameof(OrderCreatedEvent),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRepositoryThrows()
    {
        var handler = new CreateOrderCommandHandler(_orderRepository, _outboxRepository, _unitOfWork, _logger);
        var command = CreateValidCommand();

        _orderRepository.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database crash"));

        var result = await handler.Handle(command, CancellationToken.None);
        
        Assert.False(result.IsSuccess);

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    private CreateOrderCommand CreateValidCommand() => 
        new(
            Guid.NewGuid(),
            new List<OrderItemDto>
            {
               new(Guid.NewGuid(),2, 200, "USD")
                
            },
            new AddressDto("Address", "City", "Nazi Germany", "1939"),
            "Cart",
            "key");
}
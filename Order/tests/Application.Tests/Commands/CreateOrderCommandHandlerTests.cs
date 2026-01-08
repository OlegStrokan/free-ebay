using Application.Commands.CreateOrder;
using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ILogger<CreateOrderCommandHandler> _logger = Substitute.For<ILogger<CreateOrderCommandHandler>>();
    
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOrderIsCreated()
    {
        var handler = new CreateOrderCommandHandler(_orderRepository, _logger);
        var command = CreateValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);
        
        Assert.True(result.IsSuccess);

        await _orderRepository.Received(1).SaveAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRepositoryThrows()
    {
        var handler = new CreateOrderCommandHandler(_orderRepository, _logger);
        var command = CreateValidCommand();

        // setup behavior
        _orderRepository
            .When(x => x.SaveAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception("Db Error"));

        var result = await handler.Handle(command, CancellationToken.None);
        
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to create order", result.Error);
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
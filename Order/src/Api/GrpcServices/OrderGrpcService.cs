using Application.Commands.CreateOrder;
using Application.Commands.RequestReturn;
using Application.DTOs;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Microsoft.IdentityModel.Tokens.Experimental;
using Protos.Orders;

namespace Api.GrpcServices;

public class OrderGrpcService(
    IMediator mediator,
    ILogger<OrderGrpcService> logger,
    IValidator<CreateOrderRequest> createValidator,
    IValidator<RequestReturnRequest> returnValidator)
    : OrderService.OrderServiceBase
{
    public override async Task<CreateOrderResponse> CreateOrder(
        CreateOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation(
                "CreateOrder gRPC request received for customer {CustomerId} with idempotency key {IdempotencyKey}",
                request.CustomerId,
                request.IdempotencyKey);

            var validationResult = await createValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "CreateOrder validation failed: {Error}",
                    validationResult.ToString());

                return new CreateOrderResponse
                {
                    Success = false,
                    ErrorMessage = validationResult.ToString()
                };
            }

            var command = MapToCreateCommand(request);

            var result = await mediator.Send(command, context.CancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Order created successfully: {OrderId}",
                    result.Value);

                return new CreateOrderResponse
                {
                    Success = true,
                    OrderId = result.Value.ToString()
                };
            }

            logger.LogWarning("Order creation failed: {Error}",
                result.Error);

            return new CreateOrderResponse
            {
                Success = false,
                ErrorMessage = result.Error
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            return HandleException<CreateOrderResponse>(ex, "CreateOrder");
        }
    }

    public override async Task<RequestReturnResponse> RequestReturn(
        RequestReturnRequest request,
        ServerCallContext context
        )
    {
        try
        {
            logger.LogInformation(
                "RequestReturn gRPC request received for order {OrderId} with idempotency key {IdempotencyKey}",
                request.OrderId,
                request.IdempotencyKey);

            var validationResult = await returnValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "RequestReturn validation failed: {Error}",
                    validationResult.ToString());

                return new RequestReturnResponse
                {
                    Success = false,
                    ErrorMessage = validationResult.ToString()
                };
            }

            var command = new RequestReturnCommand(
                OrderId: Guid.Parse(request.OrderId),
                Reason: request.Reason,
                ItemsToReturn: request.ItemsToReturn.Select(i =>
                    new OrderItemDto(Guid.Parse(i.ProductId), i.Quantity,
                        (decimal)i.Price, i.Currency)).ToList(),
                IdempotencyKey: request.IdempotencyKey);

            var result = await mediator.Send(command, context.CancellationToken);

            return new RequestReturnResponse
            {
                Success = result.IsSuccess,
                // @think: you do can better!
                OrderId = result.IsSuccess ? result.Value.ToString() : string.Empty,
                ErrorMessage = result.Error,

            };
        }

        catch (Exception ex) when (ex is not RpcException)
        {
            return HandleException<RequestReturnResponse>(ex, "RequestReturn");

        }
    }
    
    private T HandleException<T>(Exception ex, string methodName) where T : new()
    {
        if (ex is FormatException)
        {
            logger.LogWarning(ex, "Invalid GUID format in {Method}", methodName);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID format."));
        }

        logger.LogError(ex, "Error during {Method}", methodName);
        throw new RpcException(new Status(StatusCode.Internal, $"Internal error in {methodName}"));
    }
    

    private CreateOrderCommand MapToCreateCommand(CreateOrderRequest request) => new(
        CustomerId: Guid.Parse(request.CustomerId),
        Items: request.Items.Select(i =>
            new OrderItemDto(Guid.Parse(i.ProductId), i.Quantity, (decimal)i.Price, i.Currency)).ToList(),
        DeliveryAddress: new AddressDto(
            request.DeliveryAddress.Street,
            request.DeliveryAddress.City,
            request.DeliveryAddress.Country,
            request.DeliveryAddress.PostalCode
        ),
        PaymentMethod: request.PaymentMethod,
        IdempotencyKey: request.IdempotencyKey
    );
}
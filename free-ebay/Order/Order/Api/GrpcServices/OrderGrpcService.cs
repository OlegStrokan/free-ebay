using Application.Commands.CreateOrder;
using Application.Commands.RequestReturn;
using Application.DTOs;
using Application.Interfaces;
using Application.Queries;
using Api.Mappers;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Protos.Order;

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
            var validationResult = await createValidator.ValidateAsync(request, context.CancellationToken);

            if (!validationResult.IsValid)
            {
                return new CreateOrderResponse
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
                };
            }

            var command = MapToCreateCommand(request);
            var result = await mediator.Send(command, context.CancellationToken);

            return new CreateOrderResponse
            {
                Success = result.IsSuccess,
                OrderId = result.IsSuccess ? result.Value.ToString() : string.Empty,
                ErrorMessage = result.Error ?? string.Empty
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(CreateOrder));
            throw; // unreachable: HandleException always throws, but required by the compiler
        }
    }

    public override async Task<RequestReturnResponse> RequestReturn(
        RequestReturnRequest request,
        ServerCallContext context)
    {
        try
        {
            var validationResult = await returnValidator.ValidateAsync(request, context.CancellationToken);

            if (!validationResult.IsValid)
            {
                return new RequestReturnResponse
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
                };
            }

            var command = new RequestReturnCommand(
                OrderId: Guid.Parse(request.OrderId),
                Reason: request.Reason,
                ItemsToReturn: request.ItemsToReturn.Select(i =>
                    new OrderItemDto(Guid.Parse(i.ProductId), i.Quantity, i.Price.ToDecimal(), i.Currency)).ToList(),
                IdempotencyKey: request.IdempotencyKey);

            var result = await mediator.Send(command, context.CancellationToken);

            return new RequestReturnResponse
            {
                Success = result.IsSuccess,
                ReturnRequestId = result.IsSuccess ? result.Value.ToString() : string.Empty,
                // @todo: update proto and make errorMessage non-required
                ErrorMessage = result.Error ?? string.Empty
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(RequestReturn));
            throw;
        }
    }

    private void HandleException(Exception ex, string methodName)
    {
        if (ex is FormatException)
        {
            logger.LogWarning(ex, "Invalid GUID format in {Method}", methodName);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID format."));
        }

        logger.LogError(ex, "Error during {Method}", methodName);
        throw new RpcException(new Status(StatusCode.Internal, $"Internal error in {methodName}"));
    }

    public override async Task<GetOrderResponse> GetOrder(
        GetOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.OrderId, out var orderId))
                return new GetOrderResponse { Success = false, ErrorMessage = "Invalid order ID format." };

            var result = await mediator.Send(new GetOrderQuery(orderId), context.CancellationToken);

            if (!result.IsSuccess)
                return new GetOrderResponse { Success = false, ErrorMessage = result.Error };

            return new GetOrderResponse
            {
                Success = true,
                Order = MapToOrderDetails(result.Value!)
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, "GetOrder");
            throw;
        }
    }

    public override async Task<ListOrdersResponse> ListOrders(
        ListOrdersRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new ListOrdersQuery(
                PageNumber: Math.Max(1, request.PageNumber),
                PageSize: Math.Clamp(request.PageSize, 1, 100));

            var result = await mediator.Send(query, context.CancellationToken);

            if (!result.IsSuccess)
                return new ListOrdersResponse { Success = false, ErrorMessage = result.Error };

            var response = new ListOrdersResponse { Success = true };
            response.Orders.AddRange(result.Value!.Select(MapToOrderSummary));
            return response;
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, "ListOrders");
            throw;
        }
    }

    public override async Task<GetCustomerOrdersResponse> GetCustomerOrders(
        GetCustomerOrdersRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.CustomerId, out var customerId))
                return new GetCustomerOrdersResponse { Success = false, ErrorMessage = "Invalid customer ID format." };

            var result = await mediator.Send(new GetCustomerOrdersQuery(customerId), context.CancellationToken);

            if (!result.IsSuccess)
                return new GetCustomerOrdersResponse { Success = false, ErrorMessage = result.Error };

            var response = new GetCustomerOrdersResponse { Success = true };
            response.Orders.AddRange(result.Value!.Select(MapToOrderSummary));
            return response;
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, "GetCustomerOrders");
            throw;
        }
    }

    private static OrderDetails MapToOrderDetails(OrderResponse o)
    {
        var details = new OrderDetails
        {
            Id = o.Id.ToString(),
            CustomerId = o.CustomerId.ToString(),
            TrackingId = o.TrackingId ?? string.Empty,
            PaymentId = o.PaymentId ?? string.Empty,
            Status = o.Status,
            TotalAmount = o.TotalAmount.ToDecimalValue(),
            Currency = o.Currency,
            DeliveryAddress = new Address
            {
                Street = o.DeliveryAddress.Street,
                City = o.DeliveryAddress.City,
                Country = o.DeliveryAddress.Country,
                PostalCode = o.DeliveryAddress.PostalCode
            },
            CreatedAt = o.CreatedAt.ToString("O"),
            UpdatedAt = o.UpdatedAt?.ToString("O") ?? string.Empty,
            Version = o.Version
        };

        details.Items.AddRange(o.Items.Select(i => new OrderItemDetail
        {
            ProductId = i.ProductId.ToString(),
            Quantity = i.Quantity,
            Price = i.Price.ToDecimalValue(),
            Currency = i.Currency
        }));

        return details;
    }

    private static OrderSummary MapToOrderSummary(OrderSummaryResponse o) => new()
    {
        Id = o.Id.ToString(),
        TrackingId = o.TrackingId ?? string.Empty,
        Status = o.Status,
        TotalAmount = o.TotalAmount.ToDecimalValue(),
        Currency = o.Currency,
        CreatedAt = o.CreatedAt.ToString("O")
    };
    

    private static CreateOrderCommand MapToCreateCommand(CreateOrderRequest request) => new(
        CustomerId: Guid.Parse(request.CustomerId),
        Items: request.Items.Select(i =>
            new OrderItemDto(Guid.Parse(i.ProductId), i.Quantity, i.Price.ToDecimal(), i.Currency)).ToList(),
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
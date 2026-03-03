using Api.Mappers;
using Application.Commands.RecurringOrder.CancelRecurringOrder;
using Application.Commands.RecurringOrder.CreateRecurringOrder;
using Application.Commands.RecurringOrder.PauseRecurringOrder;
using Application.Commands.RecurringOrder.ResumeRecurringOrder;
using Application.DTOs;
using Application.Queries;
using Grpc.Core;
using MediatR;
using Protos.Order;

namespace Api.GrpcServices;

public class RecurringOrderGrpcService(
    IMediator mediator,
    ILogger<RecurringOrderGrpcService> logger)
    : RecurringOrderService.RecurringOrderServiceBase
{
    public override async Task<RecurringOrderResponse> CreateRecurringOrder(
        CreateRecurringOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.CustomerId, out var customerId))
                return new RecurringOrderResponse { Success = false, ErrorMessage = "Invalid CustomerId format" };

            var items = request.Items.Select(i => new RecurringItemDto(
                ProductId: Guid.Parse(i.ProductId),
                Quantity: i.Quantity,
                Price: i.Price.ToDecimal(),
                Currency: i.Currency)).ToList();

            DateTime? firstRunAt = string.IsNullOrWhiteSpace(request.FirstRunAt)
                ? null
                : DateTime.Parse(request.FirstRunAt, null, System.Globalization.DateTimeStyles.RoundtripKind);

            int? maxExecutions = request.MaxExecutions > 0 ? request.MaxExecutions : null;

            var command = new CreateRecurringOrderCommand(
                CustomerId: customerId,
                PaymentMethod: request.PaymentMethod,
                Frequency: request.Frequency,
                Items: items,
                DeliveryAddress: MapAddress(request.DeliveryAddress),
                FirstRunAt: firstRunAt,
                MaxExecutions: maxExecutions,
                IdempotencyKey: request.IdempotencyKey);

            var result = await mediator.Send(command, context.CancellationToken);

            return result.IsSuccess
                ? new RecurringOrderResponse { Success = true, RecurringOrderId = result.Value.ToString() }
                : new RecurringOrderResponse { Success = false, ErrorMessage = result.Error ?? "Unknown error" };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(CreateRecurringOrder));
            throw;
        }
    }

    public override async Task<RecurringOrderResponse> PauseRecurringOrder(
        PauseRecurringOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.RecurringOrderId, out var id))
                return new RecurringOrderResponse { Success = false, ErrorMessage = "Invalid RecurringOrderId format" };

            var result = await mediator.Send(
                new PauseRecurringOrderCommand(id), context.CancellationToken);

            return result.IsSuccess
                ? new RecurringOrderResponse { Success = true, RecurringOrderId = request.RecurringOrderId }
                : new RecurringOrderResponse { Success = false, ErrorMessage = result.Error ?? "Unknown error" };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(PauseRecurringOrder));
            throw;
        }
    }

    public override async Task<RecurringOrderResponse> ResumeRecurringOrder(
        ResumeRecurringOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.RecurringOrderId, out var id))
                return new RecurringOrderResponse { Success = false, ErrorMessage = "Invalid RecurringOrderId format" };

            var result = await mediator.Send(
                new ResumeRecurringOrderCommand(id), context.CancellationToken);

            return result.IsSuccess
                ? new RecurringOrderResponse { Success = true, RecurringOrderId = request.RecurringOrderId }
                : new RecurringOrderResponse { Success = false, ErrorMessage = result.Error ?? "Unknown error" };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(ResumeRecurringOrder));
            throw;
        }
    }

    public override async Task<RecurringOrderResponse> CancelRecurringOrder(
        CancelRecurringOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.RecurringOrderId, out var id))
                return new RecurringOrderResponse { Success = false, ErrorMessage = "Invalid RecurringOrderId format" };

            var result = await mediator.Send(
                new CancelRecurringOrderCommand(id, request.Reason), context.CancellationToken);

            return result.IsSuccess
                ? new RecurringOrderResponse { Success = true, RecurringOrderId = request.RecurringOrderId }
                : new RecurringOrderResponse { Success = false, ErrorMessage = result.Error ?? "Unknown error" };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(CancelRecurringOrder));
            throw;
        }
    }

    public override async Task<GetRecurringOrderResponse> GetRecurringOrder(
        GetRecurringOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.RecurringOrderId, out var id))
                return new GetRecurringOrderResponse { Success = false, ErrorMessage = "Invalid RecurringOrderId format" };

            var result = await mediator.Send(
                new GetRecurringOrderQuery(id), context.CancellationToken);

            if (!result.IsSuccess)
                return new GetRecurringOrderResponse { Success = false, ErrorMessage = result.Error };

            var s = result.Value!;
            var details = new RecurringOrderDetails
            {
                Id = s.Id.ToString(),
                CustomerId = s.CustomerId.ToString(),
                PaymentMethod = s.PaymentMethod,
                Frequency = s.Frequency,
                Status = s.Status,
                NextRunAt = s.NextRunAt.ToString("O"),
                LastRunAt = s.LastRunAt?.ToString("O") ?? string.Empty,
                TotalExecutions = s.TotalExecutions,
                MaxExecutions = s.MaxExecutions ?? 0,
                DeliveryAddress = new Address
                {
                    Street = s.DeliveryAddress.Street,
                    City = s.DeliveryAddress.City,
                    Country = s.DeliveryAddress.Country,
                    PostalCode = s.DeliveryAddress.PostalCode
                },
                CreatedAt = s.CreatedAt.ToString("O"),
                UpdatedAt = s.UpdatedAt?.ToString("O") ?? string.Empty,
                Version = s.Version
            };
            details.Items.AddRange(s.Items.Select(i => new RecurringOrderItemDetail
            {
                ProductId = i.ProductId.ToString(),
                Quantity = i.Quantity,
                Price = i.Price.ToDecimalValue(),
                Currency = i.Currency
            }));

            return new GetRecurringOrderResponse { Success = true, Order = details };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(GetRecurringOrder));
            throw;
        }
    }

    public override async Task<GetCustomerRecurringOrdersResponse> GetCustomerRecurringOrders(
        GetCustomerRecurringOrdersRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.CustomerId, out var customerId))
                return new GetCustomerRecurringOrdersResponse
                    { Success = false, ErrorMessage = "Invalid CustomerId format" };

            var result = await mediator.Send(
                new GetCustomerRecurringOrdersQuery(customerId), context.CancellationToken);

            if (!result.IsSuccess)
                return new GetCustomerRecurringOrdersResponse
                    { Success = false, ErrorMessage = result.Error };

            var response = new GetCustomerRecurringOrdersResponse { Success = true };
            response.Orders.AddRange(result.Value!.Select(s => new RecurringOrderSummaryProto
            {
                Id = s.Id.ToString(),
                Frequency = s.Frequency,
                Status = s.Status,
                NextRunAt = s.NextRunAt.ToString("O"),
                TotalExecutions = s.TotalExecutions,
                CreatedAt = s.CreatedAt.ToString("O")
            }));

            return response;
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            HandleException(ex, nameof(GetCustomerRecurringOrders));
            throw;
        }
    }

    private void HandleException(Exception ex, string method)
    {
        if (ex is FormatException)
        {
            logger.LogWarning(ex, "Invalid GUID format in {Method}", method);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ID format."));
        }

        logger.LogError(ex, "Error during {Method}", method);
        throw new RpcException(new Status(StatusCode.Internal, $"Internal error in {method}"));
    }

    private static AddressDto MapAddress(Address a) =>
        new(a.Street, a.City, a.Country, a.PostalCode);
}

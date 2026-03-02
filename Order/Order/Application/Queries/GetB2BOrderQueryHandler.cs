using Application.Common;
using Application.Interfaces;
using Domain.Entities.B2BOrder;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

// @todo: go and create some new file type shit
public record GetB2BOrderQuery(Guid B2BOrderId) : IRequest<Result<B2BOrderSnapshot>>;

// Thin read projection built directly from the aggregate.
// No separate read model exists yet for B2B orders — the snapshot is the read model.
public record B2BOrderSnapshot(
    Guid Id,
    Guid CustomerId,
    string CompanyName,
    string Status,
    decimal TotalPrice,
    string Currency,
    decimal DiscountPercent,
    DateTime? RequestedDeliveryDate,
    Guid? FinalizedOrderId,
    int Version,
    List<QuoteLineItemSnapshot> Items,
    List<string> Comments,
    string Street,
    string City,
    string Country,
    string PostalCode);

public record QuoteLineItemSnapshot(
    Guid LineItemId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal? AdjustedUnitPrice,
    string Currency,
    bool IsRemoved);

public class GetB2BOrderQueryHandler(
    IB2BOrderPersistenceService persistenceService,
    ILogger<GetB2BOrderQueryHandler> logger)
    : IRequestHandler<GetB2BOrderQuery, Result<B2BOrderSnapshot>>
{
    public async Task<Result<B2BOrderSnapshot>> Handle(
        GetB2BOrderQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await persistenceService.LoadB2BOrderAsync(request.B2BOrderId, cancellationToken);

            if (order is null)
                return Result<B2BOrderSnapshot>.Failure($"B2BOrder {request.B2BOrderId} not found");

            var snapshot = MapToSnapshot(order);
            return Result<B2BOrderSnapshot>.Success(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result<B2BOrderSnapshot>.Failure("Failed to retrieve B2B order.");
        }
    }

    private static B2BOrderSnapshot MapToSnapshot(B2BOrder order)
    {
        var currency = order.ActiveItems.FirstOrDefault()?.EffectiveUnitPrice.Currency ?? "USD";
        return new B2BOrderSnapshot(
            Id: order.Id.Value,
            CustomerId: order.CustomerId.Value,
            CompanyName: order.CompanyName,
            Status: order.Status.Name,
            TotalPrice: order.TotalPrice.Amount,
            Currency: order.TotalPrice.Amount > 0 ? order.TotalPrice.Currency : currency,
            DiscountPercent: order.DiscountPercent,
            RequestedDeliveryDate: order.RequestedDeliveryDate,
            FinalizedOrderId: order.FinalizedOrderId,
            Version: order.Version,
            Items: order.ActiveItems.Select(i => new QuoteLineItemSnapshot(
                LineItemId: i.Id.Value,
                ProductId: i.ProductId.Value,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice.Amount,
                AdjustedUnitPrice: i.AdjustedUnitPrice?.Amount,
                Currency: i.UnitPrice.Currency,
                IsRemoved: i.IsRemoved)).ToList(),
            Comments: order.Comments.ToList(),
            Street: order.DeliveryAddress.Street,
            City: order.DeliveryAddress.City,
            Country: order.DeliveryAddress.Country,
            PostalCode: order.DeliveryAddress.PostalCode);
    }
}

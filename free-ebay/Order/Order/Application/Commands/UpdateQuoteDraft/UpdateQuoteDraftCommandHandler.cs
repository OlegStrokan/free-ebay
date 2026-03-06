using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities.B2BOrder;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.UpdateQuoteDraft;

public class UpdateQuoteDraftCommandHandler(
    IB2BOrderPersistenceService persistenceService,
    ILogger<UpdateQuoteDraftCommandHandler> logger)
    : IRequestHandler<UpdateQuoteDraftCommand, Result>
{
    public async Task<Result> Handle(UpdateQuoteDraftCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await persistenceService.UpdateB2BOrderAsync(
                request.B2BOrderId,
                order =>
                {
                    // Each change emits its own event - this is intentional.
                    // 50 items modified = 50 events. Snapshots absorb the cost on read.
                    foreach (var change in request.Changes)
                    {
                        ApplyChange(order, change);
                    }

                    if (!string.IsNullOrWhiteSpace(request.Comment) &&
                        !string.IsNullOrWhiteSpace(request.CommentAuthor))
                    {
                        order.AddComment(request.CommentAuthor, request.Comment);
                    }

                    return Task.CompletedTask;
                },
                cancellationToken);

            logger.LogInformation(
                "Applied {Count} change(s) to B2BOrder {B2BOrderId}",
                request.Changes.Count, request.B2BOrderId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain violation updating B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update B2BOrder {B2BOrderId}", request.B2BOrderId);
            return Result.Failure(ex.Message);
        }
    }

//@think: should it be like that?
    private static void ApplyChange(B2BOrder order, QuoteItemChangeDto change)
    {
        var productId = change.ProductId.HasValue
            ? ProductId.From(change.ProductId.Value)
            : throw new DomainException("ProductId is required for this change type");

        switch (change.Type)
        {
            case QuoteChangeType.AddItem:
                if (!change.Quantity.HasValue || !change.Price.HasValue || change.Currency is null)
                    throw new DomainException("AddItem requires Quantity, Price, and Currency");
                order.AddItem(productId, change.Quantity.Value,
                    Money.Create(change.Price.Value, change.Currency));
                break;

            case QuoteChangeType.RemoveItem:
                order.RemoveItem(productId);
                break;

            case QuoteChangeType.ChangeQuantity:
                if (!change.Quantity.HasValue)
                    throw new DomainException("ChangeQuantity requires Quantity");
                order.ChangeItemQuantity(productId, change.Quantity.Value);
                break;

            case QuoteChangeType.AdjustItemPrice:
                if (!change.Price.HasValue || change.Currency is null)
                    throw new DomainException("AdjustItemPrice requires Price and Currency");
                order.AdjustItemPrice(productId, Money.Create(change.Price.Value, change.Currency));
                break;

            default:
                throw new DomainException($"Unknown QuoteChangeType: {change.Type}");
        }
    }
}

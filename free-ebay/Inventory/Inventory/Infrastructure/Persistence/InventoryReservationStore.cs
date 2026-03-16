using System.Data;
using System.Text.Json;
using Application.Interfaces;
using Application.Models;
using Infrastructure.Messaging;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Infrastructure.Persistence;

public sealed class InventoryReservationStore(
    InventoryDbContext dbContext,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<InventoryReservationStore> logger) : IInventoryReservationStore
{
    private readonly string inventoryEventsTopic = string.IsNullOrWhiteSpace(kafkaOptions.Value.InventoryEventsTopic)
        ? "inventory.events"
        : kafkaOptions.Value.InventoryEventsTopic;

    public async Task<ReserveInventoryResult> ReserveAsync(Guid orderId, IReadOnlyCollection<ReserveStockItem> items, 
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                var result = await ReserveInternalAsync(orderId, items, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (PostgresException ex) when (ex.SqlState == "40001" && attempt < maxAttempts)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();

                logger.LogWarning(
                    ex,
                    "ReserveInventory serialization failure. Retry attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        throw new InvalidOperationException("ReserveInventory transaction failed after maximum retry attempts.");
    }

    public async Task<ReleaseInventoryResult> ReleaseAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                var result = await ReleaseInternalAsync(reservationId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (PostgresException ex) when (ex.SqlState == "40001" && attempt < maxAttempts)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();

                logger.LogWarning(
                    ex,
                    "ReleaseInventory serialization failure. Retry attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        throw new InvalidOperationException("ReleaseInventory transaction failed after maximum retry attempts.");
    }

    private async Task<ReserveInventoryResult> ReserveInternalAsync(
        Guid orderId,
        IReadOnlyCollection<ReserveStockItem> items,
        CancellationToken cancellationToken)
    {
        var existingReservation = await dbContext.InventoryReservations
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .Select(x => new { x.ReservationId })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingReservation is not null)
        {
            logger.LogInformation(
                "ReserveInventory idempotency hit. OrderId={OrderId}, ReservationId={ReservationId}",
                orderId,
                existingReservation.ReservationId);

            return ReserveInventoryResult.Successful(
                existingReservation.ReservationId.ToString(),
                isIdempotentReplay: true);
        }

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();

        var stocks = await dbContext.ProductStocks
            .Where(x => productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        if (stocks.Count != productIds.Count)
        {
            var missingProductId = productIds.First(id => !stocks.ContainsKey(id));

            return ReserveInventoryResult.Failed(
                $"Product stock not found. ProductId={missingProductId}",
                ReserveInventoryFailureReason.ProductNotFound);
        }

        foreach (var item in items)
        {
            var stock = stocks[item.ProductId];

            if (stock.AvailableQuantity < item.Quantity)
            {
                return ReserveInventoryResult.Failed(
                    $"Insufficient stock for ProductId={item.ProductId}. Available={stock.AvailableQuantity}, Requested={item.Quantity}",
                    ReserveInventoryFailureReason.InsufficientStock);
            }
        }

        var reservationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var reservation = new InventoryReservationEntity
        {
            ReservationId = reservationId,
            OrderId = orderId,
            Status = ReservationStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var item in items)
        {
            var stock = stocks[item.ProductId];

            stock.AvailableQuantity -= item.Quantity;
            stock.ReservedQuantity += item.Quantity;
            stock.UpdatedAtUtc = now;

            reservation.Items.Add(new InventoryReservationItemEntity
            {
                ReservationItemId = Guid.NewGuid(),
                ReservationId = reservationId,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });

            dbContext.InventoryMovements.Add(new InventoryMovementEntity
            {
                MovementId = Guid.NewGuid(),
                ProductId = item.ProductId,
                MovementType = MovementType.Reserve,
                QuantityDelta = -item.Quantity,
                CorrelationId = orderId.ToString(),
                CreatedAtUtc = now
            });
        }

        dbContext.InventoryReservations.Add(reservation);

        AddOutboxMessage(
            eventType: "InventoryReserved",
            payload: BuildInventoryReservedPayload(reservationId, orderId, items, now),
            createdAtUtc: now);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Inventory reserved. OrderId={OrderId}, ReservationId={ReservationId}, ItemCount={ItemCount}",
            orderId,
            reservationId,
            items.Count);

        return ReserveInventoryResult.Successful(reservationId.ToString());
    }

    private async Task<ReleaseInventoryResult> ReleaseInternalAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var reservation = await dbContext.InventoryReservations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);

        if (reservation is null)
        {
            logger.LogWarning(
                "ReleaseInventory idempotent success: reservation not found. ReservationId={ReservationId}",
                reservationId);

            return ReleaseInventoryResult.Successful(
                isIdempotentReplay: true,
                message: "Reservation not found. Treated as idempotent success.");
        }

        if (reservation.Status == ReservationStatus.Released)
        {
            logger.LogInformation(
                "ReleaseInventory idempotent success: reservation already released. ReservationId={ReservationId}",
                reservationId);

            return ReleaseInventoryResult.Successful(
                isIdempotentReplay: true,
                message: "Reservation already released.");
        }

        var now = DateTime.UtcNow;

        var productIds = reservation.Items
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var stocks = await dbContext.ProductStocks
            .Where(x => productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        foreach (var item in reservation.Items)
        {
            if (!stocks.TryGetValue(item.ProductId, out var stock))
            {
                logger.LogWarning(
                    "Stock row missing during release. ReservationId={ReservationId}, ProductId={ProductId}",
                    reservationId,
                    item.ProductId);
                continue;
            }

            stock.AvailableQuantity += item.Quantity;
            stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - item.Quantity);
            stock.UpdatedAtUtc = now;

            dbContext.InventoryMovements.Add(new InventoryMovementEntity
            {
                MovementId = Guid.NewGuid(),
                ProductId = item.ProductId,
                MovementType = MovementType.Release,
                QuantityDelta = item.Quantity,
                CorrelationId = reservationId.ToString(),
                CreatedAtUtc = now
            });
        }

        reservation.Status = ReservationStatus.Released;
        reservation.UpdatedAtUtc = now;

        AddOutboxMessage(
            eventType: "InventoryReleased",
            payload: BuildInventoryReleasedPayload(reservation, now),
            createdAtUtc: now);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Inventory released. ReservationId={ReservationId}",
            reservationId);

        return ReleaseInventoryResult.Successful();
    }

    private void AddOutboxMessage(string eventType, object payload, DateTime createdAtUtc)
    {
        dbContext.OutboxMessages.Add(new OutboxMessageEntity
        {
            OutboxMessageId = Guid.NewGuid(),
            Topic = inventoryEventsTopic,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAtUtc = createdAtUtc,
            RetryCount = 0,
            LastError = string.Empty
        });
    }

    private static object BuildInventoryReservedPayload(
        Guid reservationId,
        Guid orderId,
        IReadOnlyCollection<ReserveStockItem> items,
        DateTime occurredAtUtc)
    {
        return new
        {
            reservationId = reservationId.ToString(),
            orderId = orderId.ToString(),
            items = items.Select(x => new
            {
                productId = x.ProductId.ToString(),
                quantity = x.Quantity
            }),
            occurredAtUtc
        };
    }

    private static object BuildInventoryReleasedPayload(
        InventoryReservationEntity reservation,
        DateTime occurredAtUtc)
    {
        return new
        {
            reservationId = reservation.ReservationId.ToString(),
            orderId = reservation.OrderId.ToString(),
            items = reservation.Items.Select(x => new
            {
                productId = x.ProductId.ToString(),
                quantity = x.Quantity
            }),
            occurredAtUtc
        };
    }
}

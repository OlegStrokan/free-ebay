using System.Text.Json;
using Domain.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

public class EventStoreRepository(
    AppDbContext dbContext,
    IDomainEventTypeRegistry eventTypeRegistry,
    ILogger<EventStoreRepository> logger
) : IEventStoreRepository
{
    // optimistic concurrency type shit: checks expected version against DB, increments per event,
    // throw ex on mismatch (no auto-retry; caller must reload aggregate and retry)
    public async Task SaveEventsAsync(
        string aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any())
            return;

        var currentVersion = await GetCurrentVersionAsync(aggregateId, aggregateType, cancellationToken);

        if (currentVersion != expectedVersion)
            throw new ConcurrencyConflictException(
                aggregateType, aggregateId,
                attempts: 0); // retries are counted by the persistence service

        var nextVersion = expectedVersion + 1;

        foreach (var domainEvent in eventsList)
        {
            dbContext.DomainEvents.Add(DomainEvent.Create(
                aggregateId, aggregateType,
                domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                nextVersion++));
        }

        try
        {

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Saved {Count} events for {AggregateType} {AggregateId}",
                eventsList.Count,
                aggregateType,
                aggregateId);
        }
        catch (DbUpdateException ex) when (IsUniqueConstrainViolation(ex))
        {
            throw new ConcurrencyConflictException(
                aggregateType, aggregateId, attempts: 0);
        }
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default)
    {
        var storedEvents = await dbContext.DomainEvents
            .Where(e => e.AggregateId == aggregateId && e.AggregateType == aggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

            return DeserializeEvents(storedEvents,  aggregateId, aggregateType);
    }

    public async Task<int> GetCurrentVersionAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        var maxVersion = await dbContext.DomainEvents
            .Where(e => e.AggregateId == aggregateId && e.AggregateType == aggregateType)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return maxVersion ?? -1;
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsAfterVersionAsync(
        string aggregateId, string aggregateType, int afterVersion, CancellationToken ct = default)
    {
        var storedEvents = await dbContext.DomainEvents
            .Where(e => e.AggregateId == aggregateId
                        && e.AggregateType == aggregateType
                        && e.Version > afterVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        return DeserializeEvents(storedEvents, aggregateId, aggregateType);
    }

    private static bool IsUniqueConstrainViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == "23505";
    }

    private IEnumerable<IDomainEvent> DeserializeEvents(
        IEnumerable<DomainEvent> storedEvents,
        string aggregateId,
        string aggregateType)
    {
        var domainEvents = new List<IDomainEvent>();

        foreach (var storedEvent in storedEvents)
        {
            if (!eventTypeRegistry.TryGetType(storedEvent.EventType, out var eventType))
            {
                logger.LogWarning(
                    "Unknown event type {EventType} for {AggregateType} {AggregateId}. " +
                    "Event skipped - this usually means a deployment mismatch.",
                    storedEvent.EventType,
                    aggregateType,
                    aggregateId);
                continue;
            }
            
            var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(
                storedEvent.EventData, eventType, SerializerOptions);

            if (domainEvent is null)
            {
                logger.LogWarning(
                    "Deserialize null for event type {EventType} on {AggregateId}. Skipping.",
                    storedEvent.EventType, aggregateId);
                continue;
            }

            domainEvents.Add(domainEvent);
        }

        return domainEvents;
    }
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = 
        { 
            // @think: should we have some factory, where we loop up for file<ID> name and add it type shit?
            new StronglyTypedIdConverter<OrderId>(),
            new StronglyTypedIdConverter<OrderItemId>(),
            new StronglyTypedIdConverter<PaymentId>(),
            new StronglyTypedIdConverter<CustomerId>(),
            new StronglyTypedIdConverter<TrackingId>(),
            new StronglyTypedIdConverter<ReturnRequestId>(),
            new StronglyTypedIdConverter<RecurringOrderId>(),
            new StronglyTypedIdConverter<QuoteLineItemId>(),
            new StronglyTypedIdConverter<B2BOrderId>(),
        }
    };
}
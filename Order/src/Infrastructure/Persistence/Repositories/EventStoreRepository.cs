using System.Reflection;
using System.Text.Json;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EventStoreRepository(
    AppDbContext dbContext, 
    ILogger<EventStoreRepository> logger
    ) : IEventStoreRepository
{
    // auto-discover all event types at startup
    private static readonly Dictionary<string, Type> EventTypeMap = DiscoverEventTypes();
    
    private static Dictionary<string, Type> DiscoverEventTypes()
    {
        var eventTypes = new Dictionary<string, Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // skip system assemblies for performance
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;

            try
            {
                var types = assembly.GetTypes()
                    .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) &&
                                !t.IsInterface &&
                                !t.IsAbstract);

                foreach (var type in types)
                {
                    eventTypes[type.Name] = type;
                }
            }

            catch (ReflectionTypeLoadException)
            {
                // @think: should i delete this?
                // skip assemblies that can't be loaded
                continue;
            }
        }
        return eventTypes;
    }

    public async Task SaveEventsAsync<TId>(
        string aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any())
            return;
        
        // optimistic concurrency check
        var currentVersion = await GetCurrentVersionAsync(aggregateId, aggregateType, cancellationToken);

        if (currentVersion != expectedVersion)
            throw new InvalidOperationException(
                $"Concurrency conflict for {aggregateType} {aggregateId}. " +
                $"Expected version {expectedVersion}, but current version is {currentVersion}");

        var nextVersion = expectedVersion + 1;

        foreach (var domainEvent in eventsList)
        {
            var eventType = domainEvent.GetType().Name;
            var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

            var storedEvent = DomainEvent.Create(
                aggregateId,
                aggregateType,
                eventType,
                eventData,
                nextVersion);

            dbContext.DomainEvents.Add(storedEvent);
            nextVersion++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Saved {Count} events for {AggregateType} {AggregateId}",
            eventsList.Count,
            aggregateType,
            aggregateId);
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

        var domainEvents = new List<IDomainEvent>();

        foreach (var storedEvent in storedEvents)
        {
            if (!EventTypeMap.TryGetValue(storedEvent.EventType, out var eventType))
            {
                logger.LogWarning(
                    "Unkown event type {EventType} for {AggregateType} {AggregateId}",
                    storedEvent.EventType,
                    aggregateType,
                    aggregateId);
                continue;
            }

            var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(
                storedEvent.EventData,
                eventType)!;

            domainEvents.Add(domainEvent);
        }

        return domainEvents;
    }


    public async Task<bool> ExistsAsync(
        string aggregateId,
        string aggregateType, 
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DomainEvents
            .AnyAsync(e =>
                    e.AggregateId == aggregateId &&
                    e.AggregateType == aggregateType,
                cancellationToken);
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
}
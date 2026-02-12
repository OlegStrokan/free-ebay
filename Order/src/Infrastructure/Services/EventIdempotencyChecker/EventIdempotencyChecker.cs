using Domain.Entities;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.EventIdempotencyChecker;

// prevent duplicates for at-least-once kafka settings
public class EventIdempotencyChecker(
    AppDbContext dbContext, 
    Logger<EventIdempotencyChecker> logger) : IEventIdempotencyChecker{
    public async Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        var exists = await dbContext.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId, ct);

        if (exists)
        {
            logger.LogDebug(
                "Event {EventId} has already been processed",
                eventId);
        }

        return exists;
    }

    public async Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        try
        {
            var processedEvent = ProcessedEvent.Create(
                eventId,
                eventType,
                "KafkaConsumer"); // @think: should i use real consumer name?

            dbContext.ProcessedEvents.Add(processedEvent);
            await dbContext.SaveChangesAsync(ct);

            logger.LogDebug(
                "Marked event {EventId} ({EventType}) as processed",
                eventId,
                eventType);
        }

        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            // another consumer already marked it - this is ok (race condition, but ok)
            logger.LogInformation(
                "Event {EventId} was already marked as processed by another consumer",
                eventId);
        }
    }
}
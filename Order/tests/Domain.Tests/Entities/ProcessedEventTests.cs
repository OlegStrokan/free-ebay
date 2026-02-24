using Domain.Entities;

namespace Domain.Tests.Entities;

public class ProcessedEventTests
{
    private static readonly Guid ValidEventId = Guid.NewGuid();
    private const string ValidEventType = "OrderCreatedEvent";
    private const string ValidProcessedBy = "order-consumer-v1";

    [Fact]
    public void Create_ShouldSucceed_WhenAllParametersAreValid()
    {
        var evt = ProcessedEvent.Create(ValidEventId, ValidEventType, ValidProcessedBy);

        Assert.Equal(ValidEventId, evt.EventId);
        Assert.Equal(ValidEventType, evt.EventType);
        Assert.Equal(ValidProcessedBy, evt.ProcessedBy);
    }

    [Fact]
    public void Create_ShouldSetProcessedAt_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow;

        var evt = ProcessedEvent.Create(ValidEventId, ValidEventType, ValidProcessedBy);

        Assert.True(evt.ProcessedAt >= before);
        Assert.True(evt.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldThrow_WhenEventIdIsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ProcessedEvent.Create(Guid.Empty, ValidEventType, ValidProcessedBy));

        Assert.Contains("EventId cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenEventTypeIsNullOrWhitespace(string? eventType)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ProcessedEvent.Create(ValidEventId, eventType!, ValidProcessedBy));

        Assert.Contains("EventType is required", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenProcessedByIsNullOrWhitespace(string? processedBy)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ProcessedEvent.Create(ValidEventId, ValidEventType, processedBy!));

        Assert.Contains("ProcessedBy is required", ex.Message);
    }
    
    [Fact]
    public void Create_TwoInstancesWithSameEventId_ShouldBothExist()
    {
        var e1 = ProcessedEvent.Create(ValidEventId, ValidEventType, "consumer-1");
        var e2 = ProcessedEvent.Create(ValidEventId, ValidEventType, "consumer-2");

        // both are valid objects; idempotency enforcement is at the repository level
        Assert.Equal(e1.EventId, e2.EventId);
        Assert.NotEqual(e1.ProcessedBy, e2.ProcessedBy);
    }
}

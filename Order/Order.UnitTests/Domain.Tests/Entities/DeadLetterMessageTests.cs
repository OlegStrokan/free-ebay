using Domain.Entities;

namespace Domain.Tests.Entities;

public class DeadLetterMessageTests
{
    private static readonly Guid ValidId = Guid.NewGuid();
    private const string ValidType = "OrderCreatedEvent";
    private const string ValidContent = "{\"orderId\":\"123\"}";
    private static readonly DateTime ValidOccurredOn = DateTime.UtcNow.AddMinutes(-5);
    private const string ValidFailureReason = "Deserialization failed";
    private const int ValidRetryCount = 3;
    private const string ValidAggregateId = "AGG-001";
    
    [Fact]
    public void Create_ShouldSucceed_WhenAllParametersAreValid()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);

        Assert.Equal(ValidId, msg.Id);
        Assert.Equal(ValidType, msg.Type);
        Assert.Equal(ValidContent, msg.Content);
        Assert.Equal(ValidOccurredOn, msg.OccurredOn);
        Assert.Equal(ValidFailureReason, msg.FailureReason);
        Assert.Equal(ValidRetryCount, msg.RetryCount);
    }

    [Fact]
    public void Create_ShouldInitializeDefaultState()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);

        Assert.Equal(0, msg.DeadLetterRetryCount);
        Assert.False(msg.IsResolved);
        Assert.Null(msg.LastRetryAttempt);
        Assert.Null(msg.ResolvedAt);
        Assert.Null(msg.ResolutionNotes);
    }

    [Fact]
    public void Create_ShouldSetMovedToDeadLetterAt_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow;

        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);

        Assert.True(msg.MovedToDeadLetterAt >= before);
        Assert.True(msg.MovedToDeadLetterAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldThrow_WhenMessageIdIsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DeadLetterMessage.Create(Guid.Empty, ValidType, ValidContent, ValidOccurredOn,
                ValidFailureReason, ValidRetryCount, ValidAggregateId));

        Assert.Contains("MessageId cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenTypeIsNullOrWhitespace(string? type)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DeadLetterMessage.Create(ValidId, type!, ValidContent, ValidOccurredOn,
                ValidFailureReason, ValidRetryCount, ValidAggregateId));

        Assert.Contains("Type is required", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenContentIsNullOrWhitespace(string? content)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DeadLetterMessage.Create(ValidId, ValidType, content!, ValidOccurredOn,
                ValidFailureReason, ValidRetryCount, ValidAggregateId));

        Assert.Contains("Content is required", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenFailureReasonIsNullOrWhitespace(string? reason)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
                reason!, ValidRetryCount, ValidAggregateId));

        Assert.Contains("FailureReason is required", ex.Message);
    }
    
    [Fact]
    public void IncrementRetryCount_ShouldIncrementCounter()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);

        msg.IncrementRetryCount();

        Assert.Equal(1, msg.DeadLetterRetryCount);
    }

    [Fact]
    public void IncrementRetryCount_ShouldSetLastRetryAttempt()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);
        var before = DateTime.UtcNow;

        msg.IncrementRetryCount();

        Assert.NotNull(msg.LastRetryAttempt);
        Assert.True(msg.LastRetryAttempt >= before);
    }

    [Fact]
    public void IncrementRetryCount_MultipleTimes_ShouldAccumulate()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);

        msg.IncrementRetryCount();
        msg.IncrementRetryCount();
        msg.IncrementRetryCount();

        Assert.Equal(3, msg.DeadLetterRetryCount);
    }

    [Fact]
    public void MarkAsResolved_ShouldSetIsResolved()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);

        msg.MarkAsResolved("Manually replayed by ops team");

        Assert.True(msg.IsResolved);
    }

    [Fact]
    public void MarkAsResolved_ShouldSetResolvedAtAndNotes()
    {
        var msg = DeadLetterMessage.Create(ValidId, ValidType, ValidContent, ValidOccurredOn,
            ValidFailureReason, ValidRetryCount, ValidAggregateId);
        var before = DateTime.UtcNow;

        msg.MarkAsResolved("Fixed by replay");

        Assert.NotNull(msg.ResolvedAt);
        Assert.True(msg.ResolvedAt >= before);
        Assert.Equal("Fixed by replay", msg.ResolutionNotes);
    }
}

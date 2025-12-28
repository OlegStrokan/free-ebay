using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class TrackingIdTests
{
    [Fact]
    public void CreateUnique_ShouldGenerateNonEmptyGuid()
    {
        var emptyGuid = Guid.Empty;

        var validGuid = TrackingId.CreateUnique();
        
        Assert.NotEqual(emptyGuid, validGuid.Value);
    }

    [Fact]
    public void From_ShouldCreateInstance()
    {
        var validGuid = Guid.NewGuid();

        var createdGuid = TrackingId.From(validGuid);

        Assert.Equal(validGuid, createdGuid.Value);
    }

    [Fact]
    public void From_ShouldThrowException_WhenGuidIsEmpty()
    {
        var emptyId = Guid.Empty;

        var exception = Assert.Throws<ArgumentException>(() => TrackingId.From((emptyId)));

        Assert.Contains("TrackingId cannot be empty", exception.Message);
    }
}
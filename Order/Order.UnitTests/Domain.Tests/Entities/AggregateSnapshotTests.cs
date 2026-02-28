using Domain.Common;
using Domain.Entities;

namespace Domain.Tests.Entities;

public class AggregateSnapshotTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenAllParametersAreValid()
    {
        var snapshot = AggregateSnapshot.Create(
            aggregateId: "order-123",
            aggregateType: AggregateTypes.Order,
            version: 5,
            stateJson: "{\"status\":\"Completed\"}");

        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal("order-123", snapshot.AggregateId);
        Assert.Equal(AggregateTypes.Order, snapshot.AggregateType);
        Assert.Equal(5, snapshot.Version);
        Assert.Equal("{\"status\":\"Completed\"}", snapshot.StateJson);
    }

    [Fact]
    public void Create_ShouldSetTakenAt_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow;

        var snapshot = AggregateSnapshot.Create("id", "Type", 0, "{}");

        Assert.True(snapshot.TakenAt >= before);
        Assert.True(snapshot.TakenAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var s1 = AggregateSnapshot.Create("id1", "Type", 0, "{}");
        var s2 = AggregateSnapshot.Create("id2", "Type", 0, "{}");

        Assert.NotEqual(s1.Id, s2.Id);
    }

    [Fact]
    public void Create_ShouldAllowVersionZero()
    {
        var snapshot = AggregateSnapshot.Create("id", "Type", 0, "{}");

        Assert.Equal(0, snapshot.Version);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenAggregateIdIsNullOrWhitespace(string? aggregateId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            AggregateSnapshot.Create(aggregateId!, AggregateTypes.Order, 1, "{}"));

        Assert.Contains("AggregateId is required", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenAggregateTypeIsNullOrWhitespace(string? aggregateType)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            AggregateSnapshot.Create("id", aggregateType!, 1, "{}"));

        Assert.Contains("AggregateType is required", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ShouldThrow_WhenVersionIsNegative(int version)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            AggregateSnapshot.Create("id", AggregateTypes.Order, version, "{}"));

        Assert.Contains("Version must be >= 0", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenStateJsonIsNullOrWhitespace(string? stateJson)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            AggregateSnapshot.Create("id", AggregateTypes.Order, 1, stateJson!));

        Assert.Contains("StateJson is required", ex.Message);
    }
}

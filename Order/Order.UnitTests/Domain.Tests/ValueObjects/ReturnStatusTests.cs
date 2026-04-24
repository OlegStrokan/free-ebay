using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class ReturnStatusTests
{
    [Theory]
    [InlineData(0, "Pending")]
    [InlineData(1, "Received")]
    [InlineData(2, "Refunded")]
    [InlineData(3, "Completed")]
    public void StaticInstances_ShouldHaveCorrectNameAndValue(int expectedValue, string expectedName)
    {
        var status = ReturnStatus.FromValue(expectedValue);

        Assert.Equal(expectedName, status.Name);
        Assert.Equal(expectedValue, status.Value);
    }

    [Fact]
    public void Pending_CanTransitionTo_Received()
    {
        Assert.True(ReturnStatus.Pending.CanTransitionTo(ReturnStatus.Received));
    }

    [Fact]
    public void Received_CanTransitionTo_Refunded()
    {
        Assert.True(ReturnStatus.Received.CanTransitionTo(ReturnStatus.Refunded));
    }

    [Fact]
    public void Refunded_CanTransitionTo_Completed()
    {
        Assert.True(ReturnStatus.Refunded.CanTransitionTo(ReturnStatus.Completed));
    }

    [Theory]
    [InlineData("Pending", "Refunded")]
    [InlineData("Pending", "Completed")]
    [InlineData("Received", "Pending")]
    [InlineData("Received", "Completed")]
    [InlineData("Refunded", "Pending")]
    [InlineData("Refunded", "Received")]
    [InlineData("Completed", "Pending")]
    [InlineData("Completed", "Received")]
    [InlineData("Completed", "Refunded")]
    public void CanTransitionTo_ShouldReturnFalse_ForInvalidTransitions(string fromName, string toName)
    {
        var from = ReturnStatus.FromName(fromName);
        var to = ReturnStatus.FromName(toName);

        Assert.False(from.CanTransitionTo(to));
    }


    [Fact]
    public void ValidateTransitionTo_ShouldSucceed_ForValidTransition()
    {
        // no exception for valid transition
        ReturnStatus.Pending.ValidateTransitionTo(ReturnStatus.Received);
    }

    [Fact]
    public void ValidateTransitionTo_ShouldThrow_ForInvalidTransition()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ReturnStatus.Pending.ValidateTransitionTo(ReturnStatus.Completed));

        Assert.Contains("Cannot transition from Pending to Completed", ex.Message);
    }

    [Fact]
    public void ValidateTransitionTo_ShouldThrow_ForCompletedStatus()
    {
        // Completed is a terminal state
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ReturnStatus.Completed.ValidateTransitionTo(ReturnStatus.Pending));

        Assert.Contains("Cannot transition from Completed", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FromValue_ShouldReturnCorrectStatus(int value)
    {
        var status = ReturnStatus.FromValue(value);

        Assert.Equal(value, status.Value);
    }

    [Fact]
    public void FromValue_ShouldThrow_WhenValueIsUnknown()
    {
        Assert.Throws<ArgumentException>(() => ReturnStatus.FromValue(99));
    }
    
    [Theory]
    [InlineData("Pending")]
    [InlineData("Received")]
    [InlineData("Refunded")]
    [InlineData("Completed")]
    public void FromName_ShouldReturnCorrectStatus(string name)
    {
        var status = ReturnStatus.FromName(name);

        Assert.Equal(name, status.Name);
    }

    [Fact]
    public void FromName_ShouldThrow_WhenNameIsUnknown()
    {
        Assert.Throws<ArgumentException>(() => ReturnStatus.FromName("Unknown"));
    }
    
    [Fact]
    public void SameStatus_ShouldBeEqual()
    {
        Assert.Equal(ReturnStatus.Pending, ReturnStatus.Pending);
    }

    [Fact]
    public void DifferentStatuses_ShouldNotBeEqual()
    {
        Assert.NotEqual(ReturnStatus.Pending, ReturnStatus.Received);
        Assert.True(ReturnStatus.Pending != ReturnStatus.Received);
    }

    [Fact]
    public void FromValue_AndStaticInstance_ShouldBeEqual()
    {
        var fromValue = ReturnStatus.FromValue(1);

        Assert.Equal(ReturnStatus.Received, fromValue);
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistentWithEquals()
    {
        var a = ReturnStatus.FromValue(0);
        var b = ReturnStatus.Pending;

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void AllowedTransitions_ShouldBeIReadOnlyCollection_NotMutableHashSet()
    {
        var transitions = ReturnStatus.Pending.AllowedTransitions;

        Assert.IsAssignableFrom<IReadOnlyCollection<ReturnStatus>>(transitions);
        Assert.False(transitions is HashSet<ReturnStatus>);
    }
}

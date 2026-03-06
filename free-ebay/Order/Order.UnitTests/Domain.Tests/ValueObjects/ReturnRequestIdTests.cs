using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class ReturnRequestIdTests
{
    [Fact]
    public void CreateUnique_ShouldGenerateNonEmptyGuid()
    {
        var id = ReturnRequestId.CreateUnique();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void CreateUnique_ShouldGenerateDifferentValues_EachCall()
    {
        var id1 = ReturnRequestId.CreateUnique();
        var id2 = ReturnRequestId.CreateUnique();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void From_ShouldCreateInstance_WhenGuidIsValid()
    {
        var guid = Guid.NewGuid();

        var id = ReturnRequestId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void From_ShouldThrow_WhenGuidIsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() => ReturnRequestId.From(Guid.Empty));

        Assert.Contains("ReturnRequestId cannot be empty", ex.Message);
    }

    [Fact]
    public void TwoInstancesWithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();

        var a = ReturnRequestId.From(guid);
        var b = ReturnRequestId.From(guid);

        Assert.Equal(a, b);
    }

    [Fact]
    public void TwoInstancesWithDifferentGuids_ShouldNotBeEqual()
    {
        var a = ReturnRequestId.CreateUnique();
        var b = ReturnRequestId.CreateUnique();

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = ReturnRequestId.From(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }
}

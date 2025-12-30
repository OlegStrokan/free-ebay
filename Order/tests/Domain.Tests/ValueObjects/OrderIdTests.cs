using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class OrderIdTests
{
    [Fact]
    public void CreateUnique_ShouldGenerateNonEmptyGuid()
    {
        var emptyGuid = Guid.Empty;
        
        var generatedGuid = OrderId.CreateUnique();
        
        Assert.NotEqual(emptyGuid, generatedGuid.Value);
    }

    [Fact]
    public void From_ShouldCreateInstance()
    {
        var validGuid = Guid.NewGuid();

        var createdGuid = OrderId.From(validGuid);
        
        Assert.Equal(validGuid, createdGuid.Value);
    }

    [Fact]
    public void From_ShouldThrowException_WhenGuidIsEmpty()
    {
        var emptyGuid = Guid.Empty;

        var exception = Assert.Throws<ArgumentException>(() => OrderId.From(emptyGuid));

        Assert.Contains("OrderId cannot be empty", exception.Message);
    }
}
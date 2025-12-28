using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class CustomerIdTests
{
    [Fact]
    public void CreateUnique_ShouldGenerateNonEmptyGuid()
    {
        var emptyGuid = Guid.Empty;
        
        var customerId = CustomerId.CreateUnique();
        
        Assert.NotEqual(emptyGuid, customerId.Value);
    }

    [Fact]
    public void From_ShouldCreateInstance()
    {
        var validGuid = Guid.NewGuid();

        var customerId = CustomerId.From(validGuid);
        
        Assert.Equal(validGuid, customerId.Value);
    }

    [Fact]
    public void From_ShouldThrowException_WhenGuidEmpty()
    {
        var emptyId = Guid.Empty;

        var exception = Assert.Throws<ArgumentException>(() => CustomerId.From(emptyId));

        Assert.Contains("CustomerId cannot be empty", exception.Message);
    }

  // ToString method tests doesn't make sense
}
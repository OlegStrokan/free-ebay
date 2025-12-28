using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class ProductIdTests
{
    [Fact]
    public void CreateUnique_ShouldGenerateNonEmptyGuid()
    {
        var emptyGuid = Guid.Empty;

        var validGuid = ProductId.CreateUnique();
        
        Assert.NotEqual(emptyGuid, validGuid.Value);
    }

    [Fact]
    public void From_ShouldCreateInstance()
    {
        var validGuid = Guid.NewGuid();

        var createdGuid = ProductId.From(validGuid);

        Assert.Equal(validGuid, createdGuid.Value);
    }

    [Fact]
    public void From_ShouldThrowException_WhenGuidIsEmpty()
    {
        var emptyId = Guid.Empty;

        var exception = Assert.Throws<ArgumentException>(() => ProductId.From((emptyId)));

        Assert.Contains("ProductId cannot be empty", exception.Message);
    }
}
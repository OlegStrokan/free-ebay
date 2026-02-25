using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class OrderItemIdTests
{
    [Fact]
    public void From_ShouldCreateInstance()
    {
        var exampleId = 920L;
        
        var validId = OrderItemId.From(exampleId);
        
        Assert.Equal(exampleId, validId.Value);
    }
    

    [Fact]
    public void From_ShouldThrowException_WhenGuidIsEmpty()
    {
        var nonValidId = -10L;
        
        var exception = Assert.Throws<ArgumentException>(() => OrderItemId.From((nonValidId)));

        Assert.Contains("OrderItemId must be greater then", exception.Message);
    }
}
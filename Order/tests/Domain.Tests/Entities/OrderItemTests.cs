using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class OrderItemTests
{
    private readonly ProductId _testProductId = ProductId.CreateUnique();
    private readonly Money _validPrice = Money.Create(100, "USD");
    private const int ValidQuantity = 2;

    // create/init
    
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        var orderItem = OrderItem.Create(_testProductId, ValidQuantity, _validPrice);
        
        Assert.Equal(_testProductId, orderItem.ProductId);
        Assert.Equal(ValidQuantity, orderItem.Quantity);
        Assert.Equal(_validPrice, orderItem.PriceAtPurchase);
        // ensure placeholders are set for order aggregate to fill later 
        Assert.Null(orderItem.OrderId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidQuantity_ShouldThrowException(int invalidQuantity)
    {
        var exception =
            Assert.Throws<DomainException>(() => OrderItem.Create(_testProductId, invalidQuantity, _validPrice));

        Assert.Contains("quantity should be greater then zero", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidPrice_ShouldThrowException()
    {
        var exception =
            Assert.Throws<DomainException>(() => OrderItem.Create(_testProductId, ValidQuantity, Money.Default()));

        Assert.Contains("price should be greater then zero", exception.Message);
    }

    [Fact]
    public void InitializeOrderItem_ShouldInitialCorrectly()
    {
        var orderItem = OrderItem.Create(_testProductId, ValidQuantity, _validPrice);

        var orderId = OrderId.CreateUnique();
        var orderItemId = OrderItemId.From(10);
        orderItem.InitializeOrderItem(orderId, orderItemId);
        
        Assert.Equal(orderId, orderItem.OrderId);
        Assert.Equal(orderItemId, orderItem.Id);
    }

    [Fact]
    public void InitializeOrderItem_ShouldThrowException_WhenOrderItemAlreadyInitialize()
    {
        var orderItem = OrderItem.Create(_testProductId, ValidQuantity, _validPrice);

        var orderId = OrderId.CreateUnique();
        var orderItemId = OrderItemId.From(10);
        orderItem.InitializeOrderItem(orderId, orderItemId);

        var exception = Assert.Throws<DomainException>(() => orderItem.InitializeOrderItem(orderId, orderItemId));

        Assert.Contains("already initialized", exception.Message);
    }

    // state updates 
    
    [Fact]
    public void UpdateQuantity_WithValidValue_ShouldUpdateState()
    {
        var orderItem = OrderItem.Create(_testProductId, ValidQuantity, _validPrice);
        
        orderItem.UpdateQuantity(10);
        
        Assert.Equal(10, orderItem.Quantity);
    }

    [Fact]
    public void UpdatePrice_WithValuePrice_ShouldUpdateState()
    {
        var orderItem = OrderItem.Create(_testProductId, ValidQuantity, _validPrice);
        var newPrice = Money.Create(200, "USD");
        
        orderItem.UpdatePrice(newPrice);

        Assert.Equal(newPrice, orderItem.PriceAtPurchase);
    }
    
    // domain logic

    [Fact]
    public void GetSubTotal_ShouldCalculatePriceCorrectly()
    {
        var quantity = 3;
        var orderItem = OrderItem.Create(_testProductId, quantity, _validPrice);
        var expectedSubTotal = Money.Create(300, "USD");

        var result = orderItem.GetSubTotal();
        
        Assert.Equal(expectedSubTotal, result);
    }

    [Fact]
    public void IsPriceValid_ShouldReturnTrueForPositivePrice()
    {
        var orderItem = OrderItem.Create(_testProductId, ValidQuantity, _validPrice);
        
        Assert.True(orderItem.IsPriceValid());
    }


    [Fact]
    public void Builder_ShouldCreateValidOrderItem()
    {
        var orderItem = OrderItem.Builder.Build(_testProductId, ValidQuantity, _validPrice);

        Assert.NotNull(orderItem);
        Assert.Equal(_testProductId, orderItem.ProductId);
    }
}
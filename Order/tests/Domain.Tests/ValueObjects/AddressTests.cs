using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class AddressTests
{
    [Theory]
    [InlineData("", "city", "state", "country", "postalCode")]
    [InlineData("   ", "city", "state", "country", "postalCode")]
    [InlineData(null, "city", "state", "country", "postalCode")]
    
    [InlineData("street", "", "state", "country", "postalCode")]
    [InlineData("street", "   ", "state", "country", "postalCode")]
    [InlineData("street", null, "state", "country", "postalCode")]
    
    [InlineData("street", "city", "", "country", "postalCode")]
    [InlineData("street", "city", "   ", "country", "postalCode")]
    [InlineData("street", "city", null, "country", "postalCode")]
    
    [InlineData("street", "city", "state", "", "postalCode")]
    [InlineData("street", "city", "state", "   ", "postalCode")]
    [InlineData("street", "city", "state", null, "postalCode")]
    
    [InlineData("street", "city", "state", "country", "")]
    [InlineData("street", "city", "state", "country", "   ")]
    [InlineData("street", "city", "state", "country", null)]
    public void Create_ShouldThrowException_WhenRequiredFieldIsMissing(
        string street, string city, string state, string country, string postalCode)
    {
        Assert.Throws<ArgumentException>(() => Address.Create(street, city, state, country, postalCode));
    }
}
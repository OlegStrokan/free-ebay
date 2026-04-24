using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class AddressTests
{
    [Theory]
    [InlineData("", "city", "country", "postalCode")]
    [InlineData("   ", "city", "country", "postalCode")]
    [InlineData(null, "city",  "country", "postalCode")]
    
    [InlineData("street", "",  "country", "postalCode")]
    [InlineData("street", "   ", "country", "postalCode")]
    [InlineData("street", null, "country", "postalCode")]
 
    [InlineData("street", "city", "", "postalCode")]
    [InlineData("street", "city",  "   ", "postalCode")]
    [InlineData("street", "city", null, "postalCode")]
    
    [InlineData("street", "city", "country", "")]
    [InlineData("street", "city",  "country", "   ")]
    [InlineData("street", "city", "country", null)]
    public void Create_ShouldThrowException_WhenRequiredFieldIsMissing(
        string street, string city, string country, string postalCode)
    {
        Assert.Throws<ArgumentException>(() => Address.Create(street, city, country, postalCode));
    }
}
using Domain.Services;

namespace Domain.Tests.Services;

public class ReturnPolicyServiceTests
{
    private readonly ReturnPolicyService _sut = new();

    // helper to build a context with safe defaults
    private static ReturnPolicyContext Build(
        string countryCode = "US",
        List<string>? categories = null,
        string customerTier = "Standard",
        bool isHolidaySeason = false) =>
        new(countryCode, categories ?? new List<string>(), customerTier, isHolidaySeason);


    [Fact]
    public void CalculateReturnWindow_ShouldReturn7Days_WhenNoModifiersApply()
    {
        var ctx = Build();

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(7), window);
    }
    
    [Theory]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("PL")]
    [InlineData("CZ")]
    [InlineData("de")] // case-insensitive
    public void CalculateReturnWindow_ShouldReturn14Days_ForEuCountry(string countryCode)
    {
        var ctx = Build(countryCode: countryCode);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(14), window);
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn7Days_ForNonEuCountry()
    {
        var ctx = Build(countryCode: "US");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(7), window);
    }
    
    [Fact]
    public void CalculateReturnWindow_ShouldReturn21Days_ForSubscriberTier()
    {
        // Subscriber tier applies Max(base, 21) - Max(7, 21) = 21
        var ctx = Build(customerTier: "Subscriber");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(21), window);
    }
    
    [Fact]
    public void CalculateReturnWindow_ShouldReturn37Days_ForPremiumTier()
    {
        var ctx = Build(customerTier: "Premium");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(37), window); // 7 + 30
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn21Days_WhenHolidaySeason()
    {
        var ctx = Build(isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(21), window); // 7 + 14
    }
    
    [Fact]
    public void CalculateReturnWindow_ShouldReturn51Days_ForPremiumDuringHoliday()
    {
        var ctx = Build(customerTier: "Premium", isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(51), window); // 7 + 30 + 14
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn44Days_ForEuPremium()
    {
        var ctx = Build(countryCode: "FR", customerTier: "Premium");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(44), window); // Max(7,14)=14, then +30
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn58Days_ForPremiumEuDuringHoliday()
    {
        // EU: Max(7,14)=14; Premium: +30; Holiday: +14 => 58 days
        var ctx = Build(countryCode: "DE", customerTier: "Premium", isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(58), window); // 14 + 30 + 14
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn35Days_ForSubscriberDuringHoliday()
    {
        // Subscriber: Max(7, 21) = 21; Holiday: +14 → 35
        var ctx = Build(customerTier: "Subscriber", isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(35), window);
    }
}

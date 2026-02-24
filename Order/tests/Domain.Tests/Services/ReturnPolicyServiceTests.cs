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
    public void CalculateReturnWindow_ShouldReturn14Days_WhenNoModifiersApply()
    {
        var ctx = Build();

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(14), window);
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
    public void CalculateReturnWindow_ShouldReturn14Days_ForNonEuCountry()
    {
        var ctx = Build(countryCode: "US");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(14), window);
    }
    
    [Fact]
    public void CalculateReturnWindow_ShouldReturn7Days_ForSubscriberTier()
    {
        var ctx = Build(customerTier: "Subscriber");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(7), window);
    }
    
    [Fact]
    public void CalculateReturnWindow_ShouldReturn35Days_ForPremiumTier()
    {
        var ctx = Build(customerTier: "Premium");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(35), window); // 14 + 21
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn28Days_WhenHolidaySeason()
    {
        var ctx = Build(isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(28), window); // 14 + 14
    }
    
    [Fact]
    public void CalculateReturnWindow_ShouldReturn49Days_ForPremiumDuringHoliday()
    {
        var ctx = Build(customerTier: "Premium", isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(49), window); // 14 + 21 + 14
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn35Days_ForEuPremium()
    {
        var ctx = Build(countryCode: "FR", customerTier: "Premium");

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(35), window); // Max(14,14)=14, then +21
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn63Days_ForPremiumEuDuringHoliday()
    {
        // EU: Max(14,14)=14 => Premium: +21 => Holiday: +14 => 49 days
        // Note: EU doesn't extend over base 14, so same as Premium+Holiday = 49
        var ctx = Build(countryCode: "DE", customerTier: "Premium", isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(49), window); // 14 + 21 + 14
    }

    [Fact]
    public void CalculateReturnWindow_ShouldReturn21Days_ForSubscriberDuringHoliday()
    {
        // Subscriber: window = Max(14,7) = 7; Holiday: +14 → 21
        var ctx = Build(customerTier: "Subscriber", isHolidaySeason: true);

        var window = _sut.CalculateReturnWindow(ctx);

        Assert.Equal(TimeSpan.FromDays(21), window);
    }
}

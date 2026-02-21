namespace Domain.Services;

// @todo

public record ReturnPolicyContext(
    string CountryCode,
    List<string> ProductCategories,
    string CustomerTier,
    bool IsHolidaySeason
);


public class ReturnPolicyService
{
    // @think: you can do better
    private static readonly HashSet<string> EuCountries = new() { "DE", "FR", "PL", "ES", "IT", "CZ", "NL", "AT" }; 
    public TimeSpan CalculateReturnWindow(ReturnPolicyContext context)
    {
        var window = TimeSpan.FromDays(14);
        
        if (EuCountries.Contains(context.CountryCode.ToUpper()))
            window = Max(window, TimeSpan.FromDays(14));

        if (context.CustomerTier == "Subscriber")
            window = Max(window, TimeSpan.FromDays(7));

        if (context.CustomerTier == "Premium")
            window = Add(window, TimeSpan.FromDays(21));

        if (context.IsHolidaySeason)
            window = Add(window, TimeSpan.FromDays(14));

        return window;
    }
    
    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a < b ? a : b;
    private static TimeSpan Add(TimeSpan base_, TimeSpan extra) => base_ + extra;
}

namespace Domain.Services;

// @todo

public record ReturnPolicyContext(
    string countryCode,
    List<string> productCategories,
    string CustomerTier,
    bool isHolidaySeason
);


public class ReturnPolicyService
{
    public void calculateReturnWindow(ReturnPolicyContext returnPolicyContext)
    {
        
    }
}

// if euCountries.has(countryCode) => 14 days mandatory
// if customer.isSubscriber() => add 1 week to window + free shipping only if customer live in the save country 
// if customer.isPremium() => add 2-4 weeks to window + free shipping
// if 
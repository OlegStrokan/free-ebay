namespace payment_service.Entities;

public class MoneyEntity
{
    public int Amount { get; set; }          
    public string Currency { get; set; } = "usd";
    public int Fraction { get; set; }       

    public MoneyEntity() { }

    public long ToStripeAmount()
    {
        return Amount * 100 + Fraction;
    }
}
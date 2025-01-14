namespace payment_service.Entities;

public class Money
{
    public int Amount { get; set; }
    public string Currency { get; set; }
    public int Fraction { get; set; }

    public Money(int amount, string currency, int fraction)
    {
        Amount = amount;
        Currency = currency;
        Fraction = fraction;
    }

    public string Format()
    {
        return $"{Amount} {Currency} {Fraction}";
    }
}
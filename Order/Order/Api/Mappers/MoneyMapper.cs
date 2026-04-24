using Domain.ValueObjects;
using Protos.Common;

namespace Api.Mappers;

public static class MoneyMapper
{
    private const decimal NanoFactor = 1_000_000_000m;

    public static DecimalValue ToDecimalValue(this Money money)
        => money.Amount.ToDecimalValue();

    public static DecimalValue ToDecimalValue(this decimal value)
    {
        var units = decimal.Truncate(value);
        var nanos = (int)((value - units) * NanoFactor);
        return new DecimalValue { Units = (long)units, Nanos = nanos };
    }

    public static decimal ToDecimal(this DecimalValue grpcValue)
        => grpcValue.Units + (grpcValue.Nanos / NanoFactor);

    public static Money ToDomain(this DecimalValue grpcValue, string currency)
    {
        if (grpcValue == null) return Money.Default(currency);

        decimal amount = grpcValue.ToDecimal();
        return Money.Create(amount, currency);
    }
}
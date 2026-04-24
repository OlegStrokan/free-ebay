using Protos.Common;

namespace Infrastructure.Extensions;

public static class DecimalValueExtensions
{
    private const decimal NanoFactor = 1_000_000_000m;

    public static DecimalValue ToDecimalValue(this decimal value)
    {
        var units = decimal.Truncate(value);
        var nanos = (int)((value - units) * NanoFactor);
        return new DecimalValue { Units = (long)units, Nanos = nanos };
    }

    public static decimal ToDecimal(this DecimalValue grpcValue)
        => grpcValue.Units + (grpcValue.Nanos / NanoFactor);
}

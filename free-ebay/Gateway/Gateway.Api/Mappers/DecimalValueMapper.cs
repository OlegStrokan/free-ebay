namespace Gateway.Api.Mappers;

public static class DecimalValueMapper
{
    public static decimal ToDecimal(Protos.Common.DecimalValue? value)
    {
        if (value is null) return 0m;
        return value.Units + value.Nanos / 1_000_000_000m;
    }

    public static Protos.Common.DecimalValue ToProto(decimal value)
    {
        var units = (long)value;
        var nanos = (int)((value - units) * 1_000_000_000m);
        return new Protos.Common.DecimalValue { Units = units, Nanos = nanos };
    }
}

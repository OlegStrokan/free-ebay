namespace Domain.ValueObjects;

public sealed class ScheduleFrequency
{
    public static readonly ScheduleFrequency Weekly = new("Weekly",7);
    public static readonly ScheduleFrequency BiWeekly = new("BiWeekly",14);
    public static readonly ScheduleFrequency Monthly = new("Monthly",30);
    public static readonly ScheduleFrequency Quarterly = new("Quarterly",90);

    public string Name { get; }
    public int IntervalDays { get; }

    private ScheduleFrequency(string name, int intervalDays)
    {
        Name = name;
        IntervalDays = intervalDays;
    }

    public static ScheduleFrequency Custom(int intervalDays)
    {
        if (intervalDays < 1)
            throw new ArgumentException("Interval must be at least 1 day", nameof(intervalDays));
        return new ScheduleFrequency($"Every{intervalDays}Days", intervalDays);
    }

    public static ScheduleFrequency FromName(string name) => name switch
    {
        "Weekly"    => Weekly,
        "BiWeekly"  => BiWeekly,
        "Monthly"   => Monthly,
        "Quarterly" => Quarterly,
        _ when name.StartsWith("Every", StringComparison.Ordinal)
               && name.EndsWith("Days", StringComparison.Ordinal)
               && int.TryParse(name[5..^4], out var days) => Custom(days),
        _ => throw new ArgumentException($"Unknown ScheduleFrequency: '{name}'", nameof(name))
    };

    public static DateTime CalculateNextRunAt(DateTime from, string frequency) => frequency switch
    {
        "Weekly" => from.AddDays(7),
        "BiWeekly" => from.AddDays(14),
        "Monthly" => from.AddMonths(1),
        "Quarterly" => from.AddMonths(3),
        _ when frequency.StartsWith("Every", StringComparison.Ordinal) => 
            from.AddDays(int.Parse(frequency[5..^4])),
        _ => throw new InvalidOperationException($"Unknown frequency: {frequency}")
    };

    public override string ToString() => Name;
}

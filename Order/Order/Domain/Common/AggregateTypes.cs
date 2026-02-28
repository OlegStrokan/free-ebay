namespace Domain.Common;

/// <summary>
/// Constants for aggregate type names used in the event store, outbox, and snapshots.
/// Having them in one place prevents silent typo-bugs where two services disagree on the type name.
/// </summary>
public static class AggregateTypes
{
    public const string Order = "Order";
    public const string ReturnRequest = "ReturnRequest";
    public const string B2BOrder = "B2BOrder";
}

namespace Application.Interfaces;

public sealed record WriteRegionOwnershipDecision(
    bool IsEnabled,
    bool IsCurrentRegionOwner,
    string CurrentRegion,
    string OwnerRegion);

public interface IWriteRegionOwnershipResolver
{
    WriteRegionOwnershipDecision ResolveForCustomer(Guid customerId);
}

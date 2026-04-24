using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class DeterministicWriteRegionOwnershipResolver(
    IOptions<WriteRoutingOptions> options)
    : IWriteRegionOwnershipResolver
{
    public WriteRegionOwnershipDecision ResolveForCustomer(Guid customerId)
    {
        var cfg = options.Value;

        if (!cfg.Enabled)
        {
            return new WriteRegionOwnershipDecision(
                IsEnabled: false,
                IsCurrentRegionOwner: true,
                CurrentRegion: cfg.CurrentRegion,
                OwnerRegion: cfg.CurrentRegion);
        }

        if (string.IsNullOrWhiteSpace(cfg.CurrentRegion) || cfg.Regions.Count == 0)
        {
            return new WriteRegionOwnershipDecision(
                IsEnabled: false,
                IsCurrentRegionOwner: true,
                CurrentRegion: cfg.CurrentRegion,
                OwnerRegion: cfg.CurrentRegion);
        }

        var regions = cfg.Regions
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (regions.Length == 0)
        {
            return new WriteRegionOwnershipDecision(
                IsEnabled: false,
                IsCurrentRegionOwner: true,
                CurrentRegion: cfg.CurrentRegion,
                OwnerRegion: cfg.CurrentRegion);
        }

        var index = GetStableIndex(customerId, regions.Length);
        var ownerRegion = regions[index];
        var isOwner = string.Equals(
            cfg.CurrentRegion,
            ownerRegion,
            StringComparison.OrdinalIgnoreCase);

        return new WriteRegionOwnershipDecision(
            IsEnabled: true,
            IsCurrentRegionOwner: isOwner,
            CurrentRegion: cfg.CurrentRegion,
            OwnerRegion: ownerRegion);
    }

    private static int GetStableIndex(Guid customerId, int bucketCount)
    {
        var bytes = customerId.ToByteArray();
        var value = BitConverter.ToUInt32(bytes, 0);
        return (int)(value % (uint)bucketCount);
    }
}

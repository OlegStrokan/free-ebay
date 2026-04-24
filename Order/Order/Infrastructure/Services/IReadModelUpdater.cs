using Domain.Common;

namespace Infrastructure.Services;

/// <see cref="CanHandle"/> lets the Kafka synchronizer route events without fragile string matching.
public interface IReadModelUpdater
{
    bool CanHandle(Type eventType);
}

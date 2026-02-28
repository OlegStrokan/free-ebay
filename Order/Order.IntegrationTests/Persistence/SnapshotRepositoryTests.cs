using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Persistence;

[Collection("Integration")]
public sealed class SnapshotRepositoryTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public SnapshotRepositoryTests(IntegrationFixture fixture) => _fixture = fixture;
    
    [Fact]
    public async Task SnapshotRepository_SaveAndLoad_RoundTrip()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

        var aggregateId = Guid.NewGuid().ToString();
        // representative state blob - same structure used by OrderPersistenceService
        const string stateJson = """{"orderId":"some-id","status":2,"version":50}""";

        var snapshot = AggregateSnapshot.Create(aggregateId, AggregateTypes.Order, version: 50, stateJson);

        await repo.SaveAsync(snapshot, CancellationToken.None);

        var loaded = await repo.GetLatestAsync(aggregateId, AggregateTypes.Order, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.AggregateId.Should().Be(aggregateId);
        loaded.AggregateType.Should().Be(AggregateTypes.Order);
        loaded.Version.Should().Be(50);
        loaded.StateJson.Should().Be(stateJson);
    }
    
    [Fact]
    public async Task GetLatestAsync_ShouldReturnHighestVersion_WhenMultipleSnapshotsExist()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

        var aggregateId = Guid.NewGuid().ToString();

        // insert in shitty order to prove selection is by version, not insertion time
        await repo.SaveAsync(AggregateSnapshot.Create(aggregateId, AggregateTypes.Order, 50,  """{"v":50}"""),  CancellationToken.None);
        await repo.SaveAsync(AggregateSnapshot.Create(aggregateId, AggregateTypes.Order, 100, """{"v":100}"""), CancellationToken.None);
        await repo.SaveAsync(AggregateSnapshot.Create(aggregateId, AggregateTypes.Order, 75,  """{"v":75}"""),  CancellationToken.None);

        var latest = await repo.GetLatestAsync(aggregateId, AggregateTypes.Order, CancellationToken.None);

        latest.Should().NotBeNull();
        latest!.Version.Should().Be(100);
        latest.StateJson.Should().Be("""{"v":100}""");
    }
    
    [Fact]
    public async Task SaveAsync_ShouldBeIdempotent_WhenSameVersionSavedTwice()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

        var aggregateId = Guid.NewGuid().ToString();
        var snapshot = AggregateSnapshot.Create(aggregateId, AggregateTypes.Order, version: 50, """{"v":50}""");

        await repo.SaveAsync(snapshot, CancellationToken.None);
        
        // "retry" - must not throw and must not create a duplicate row
        await repo.SaveAsync(snapshot, CancellationToken.None);

        await using var verifyScope = _fixture.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var count = await db.AggregateSnapshots
            .CountAsync(s => s.AggregateId == aggregateId && s.AggregateType == AggregateTypes.Order);

        count.Should().Be(1, "a repeated save of the same version must be a no-op");
    }
}

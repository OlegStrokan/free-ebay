using Application.Sagas;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Sagas;

[Collection("Integration")]
public sealed class SagaDistributedLockTests : IClassFixture<IntegrationFixture>
{
    private readonly ISagaDistributedLock _lock;

    public SagaDistributedLockTests(IntegrationFixture fixture)
    {
        _lock = fixture.Services.GetRequiredService<ISagaDistributedLock>();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnHandle_WhenKeyIsFree()
    {
        var key = $"test-lock:{Guid.NewGuid()}";

        await using var handle = await _lock.TryAcquireAsync(key, TimeSpan.FromSeconds(10), CancellationToken.None);

        handle.Should().NotBeNull("the key is free so the lock must be granted");
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenKeyIsAlreadyHeld()
    {
        var key = $"test-lock:{Guid.NewGuid()}";

        // First caller holds the lock
        await using var firstHandle = await _lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), CancellationToken.None);
        firstHandle.Should().NotBeNull();
        
        // Second caller for the same key must be rejected (so this is we we dont use "await using")
        var secondHandle = await _lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), CancellationToken.None);
        secondHandle.Should().BeNull("the lock is already held by the first caller");

        if (secondHandle != null) await secondHandle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldSucceed_AfterPreviousLockIsReleased()
    {
        var key = $"test-lock:{Guid.NewGuid()}";

        // Acquire and then release
        var firstHandle = await _lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), CancellationToken.None);
        firstHandle.Should().NotBeNull();
        await firstHandle!.DisposeAsync();

        // Now a new caller should be able to acquire
        await using var secondHandle = await _lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), CancellationToken.None);
        secondHandle.Should().NotBeNull("the lock was released so a new acquisition must succeed");
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldIsolate_DifferentKeys()
    {
        var key1 = $"test-lock:{Guid.NewGuid()}";
        var key2 = $"test-lock:{Guid.NewGuid()}";

        await using var handle1 = await _lock.TryAcquireAsync(key1, TimeSpan.FromSeconds(30), CancellationToken.None);
        await using var handle2 = await _lock.TryAcquireAsync(key2, TimeSpan.FromSeconds(30), CancellationToken.None);

        handle1.Should().NotBeNull("different keys are independent");
        handle2.Should().NotBeNull("different keys are independent");
    }

    [Fact]
    public async Task ConcurrentAcquire_OnSameKey_ShouldGrantLockToExactlyOneCaller()
    {
        var key = $"test-lock:{Guid.NewGuid()}";

        // Fire 10 concurrent TryAcquireAsync calls for the same key
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var acquiredCount = results.Count(h => h != null);

        // Clean up
        foreach (var handle in results.OfType<ISagaLockHandle>())
            await handle.DisposeAsync();

        acquiredCount.Should().Be(1, "only one caller must win the Redis SETNX race");
    }
}

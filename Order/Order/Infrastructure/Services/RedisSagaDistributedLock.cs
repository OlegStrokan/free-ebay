using Application.Sagas;
using StackExchange.Redis;

namespace Infrastructure.Services;

/* Redis distributed lock type shit for saga execution.
 * atomic, set if not exists with TTL
 * the per-lock random value prevents a slow saga from accidentally releasing
 * a lock that was re-acquired by a different caller after the TTL expired.
 */
public sealed class RedisSagaDistributedLock(IConnectionMultiplexer redis) : ISagaDistributedLock
{
    public async Task<ISagaLockHandle?> TryAcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var lockValue = Guid.NewGuid().ToString("N");

        var acquired = await db.StringSetAsync(
            key,
            lockValue,
            expiry,
            when: When.NotExists);

        return acquired
            ? new RedisLockHandle(db, key, lockValue)
            : null;
    }
}

// Releases the Redis lock on dispose using an atomic Lua compare-and-delete
// This guarantees we never release a lock that was re-acquired by another caller
internal sealed class RedisLockHandle : ISagaLockHandle
{
    // returns 1 if deleted, 0 if key is already gone or someone else OWN this
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then " +
        "  return redis.call('del', KEYS[1]) " +
        "else " +
        "  return 0 " +
        "end";

    private readonly IDatabase _db;
    private readonly string _key;
    private readonly string _value;
    private bool _disposed;

    internal RedisLockHandle(IDatabase db, string key, string value)
    {
        _db = db;
        _key = key;
        _value = value;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _db.ScriptEvaluateAsync(
            ReleaseScript,
            keys: [new RedisKey(_key)],
            values: [new RedisValue(_value)]);
    }
}

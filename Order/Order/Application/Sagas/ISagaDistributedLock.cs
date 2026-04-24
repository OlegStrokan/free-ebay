namespace Application.Sagas;

public interface ISagaLockHandle : IAsyncDisposable { }

public interface ISagaDistributedLock
{
    Task<ISagaLockHandle?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken);
}

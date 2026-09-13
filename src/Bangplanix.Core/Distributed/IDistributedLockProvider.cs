namespace Bangplanix.Core.Distributed;

/// <summary>
/// Distributed lease token representing ownership of a named lock.
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
    string ResourceKey { get; }
    string LockId { get; }
    DateTime AcquiredAtUtc { get; }
    DateTime ExpiresAtUtc { get; }
    bool IsAcquired { get; }
}

/// <summary>
/// Abstraction for distributed leader election and mutex locking across Kubernetes multi-pod clusters.
/// </summary>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Attempts to acquire an exclusive lock on a named resource.
    /// </summary>
    Task<IDistributedLockHandle?> TryAcquireLockAsync(
        string resourceKey,
        TimeSpan leaseDuration,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an existing lock handle.
    /// </summary>
    Task ReleaseLockAsync(IDistributedLockHandle handle, CancellationToken cancellationToken = default);
}

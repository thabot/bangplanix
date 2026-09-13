using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Bangplanix.Core.Caching;

public class CacheItem<T>
{
    public required T Value { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}

public interface IDistributedCacheFallback
{
    ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync(string key, byte[] value, TimeSpan ttl, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public class InMemoryDistributedFallback : IDistributedCacheFallback
{
    private readonly ConcurrentDictionary<string, CacheItem<byte[]>> _store = new(StringComparer.Ordinal);

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var item))
        {
            if (item.ExpiresAtUtc > DateTime.UtcNow)
            {
                return ValueTask.FromResult<byte[]?>(item.Value);
            }
            _store.TryRemove(key, out _);
        }
        return ValueTask.FromResult<byte[]?>(null);
    }

    public ValueTask SetAsync(string key, byte[] value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _store[key] = new CacheItem<byte[]>
        {
            Value = value,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
        };
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}

public class TwoTierReportCache
{
    private readonly ConcurrentDictionary<string, CacheItem<byte[]>> _l1Cache = new(StringComparer.Ordinal);
    private readonly IDistributedCacheFallback _l2Cache;
    private readonly TimeSpan _defaultL1Ttl;
    private readonly TimeSpan _defaultL2Ttl;

    private long _l1HitCount;
    private long _l2HitCount;
    private long _missCount;

    public long L1HitCount => Interlocked.Read(ref _l1HitCount);
    public long L2HitCount => Interlocked.Read(ref _l2HitCount);
    public long MissCount => Interlocked.Read(ref _missCount);

    public TwoTierReportCache(
        IDistributedCacheFallback? l2Cache = null,
        TimeSpan? defaultL1Ttl = null,
        TimeSpan? defaultL2Ttl = null)
    {
        _l2Cache = l2Cache ?? new InMemoryDistributedFallback();
        _defaultL1Ttl = defaultL1Ttl ?? TimeSpan.FromMinutes(10);
        _defaultL2Ttl = defaultL2Ttl ?? TimeSpan.FromHours(2);
    }

    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Tier 1: Check In-Memory Fast LRU Cache
        if (_l1Cache.TryGetValue(key, out var l1Item))
        {
            if (l1Item.ExpiresAtUtc > DateTime.UtcNow)
            {
                Interlocked.Increment(ref _l1HitCount);
                return l1Item.Value;
            }
            _l1Cache.TryRemove(key, out _);
        }

        // Tier 2: Check Distributed Cache (Redis)
        var l2Bytes = await _l2Cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (l2Bytes != null)
        {
            Interlocked.Increment(ref _l2HitCount);
            // Populate L1 cache
            _l1Cache[key] = new CacheItem<byte[]>
            {
                Value = l2Bytes,
                ExpiresAtUtc = DateTime.UtcNow.Add(_defaultL1Ttl)
            };
            return l2Bytes;
        }

        Interlocked.Increment(ref _missCount);
        return null;
    }

    public async ValueTask SetAsync(string key, byte[] data, TimeSpan? l1Ttl = null, TimeSpan? l2Ttl = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(data);

        var l1Duration = l1Ttl ?? _defaultL1Ttl;
        var l2Duration = l2Ttl ?? _defaultL2Ttl;

        // Set L1
        _l1Cache[key] = new CacheItem<byte[]>
        {
            Value = data,
            ExpiresAtUtc = DateTime.UtcNow.Add(l1Duration)
        };

        // Set L2
        await _l2Cache.SetAsync(key, data, l2Duration, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _l1Cache.TryRemove(key, out _);
        await _l2Cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }
}

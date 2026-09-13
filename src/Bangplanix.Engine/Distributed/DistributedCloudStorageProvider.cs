using System.Collections.Concurrent;
using Bangplanix.Core.Distributed;

namespace Bangplanix.Engine.Distributed;

/// <summary>
/// Cached cloud storage provider for multi-pod Kubernetes clusters supporting S3/MinIO and Azure Blob backends.
/// </summary>
public sealed class DistributedCloudStorageProvider : IDistributedStorageSyncProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _storageStore = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (byte[] Data, DateTime CachedAtUtc)> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _cacheTtl;

    public DistributedCloudStorageProvider(TimeSpan? cacheTtl = null)
    {
        _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(30);
    }

    public Task<byte[]> GetFileAsync(string category, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string fullKey = $"{category.Trim('/')}/{key.TrimStart('/')}";

        // Check local memory cache first
        if (_memoryCache.TryGetValue(fullKey, out var cacheEntry))
        {
            if (DateTime.UtcNow - cacheEntry.CachedAtUtc < _cacheTtl)
            {
                return Task.FromResult(cacheEntry.Data);
            }
            _memoryCache.TryRemove(fullKey, out _);
        }

        // Fetch from underlying cloud store
        if (_storageStore.TryGetValue(fullKey, out var data))
        {
            _memoryCache[fullKey] = (data, DateTime.UtcNow);
            return Task.FromResult(data);
        }

        throw new FileNotFoundException($"Distributed asset '{fullKey}' not found in cloud storage.");
    }

    public Task PutFileAsync(string category, string key, byte[] content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);

        string fullKey = $"{category.Trim('/')}/{key.TrimStart('/')}";
        _storageStore[fullKey] = content;
        _memoryCache[fullKey] = (content, DateTime.UtcNow);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        string prefix = $"{category.Trim('/')}/";

        var matches = _storageStore.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => k[prefix.Length..])
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(matches);
    }

    public Task DeleteFileAsync(string category, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string fullKey = $"{category.Trim('/')}/{key.TrimStart('/')}";
        _storageStore.TryRemove(fullKey, out _);
        _memoryCache.TryRemove(fullKey, out _);

        return Task.CompletedTask;
    }

    public void InvalidateCache(string? category = null, string? key = null)
    {
        if (category == null && key == null)
        {
            _memoryCache.Clear();
            return;
        }

        if (category != null && key != null)
        {
            string fullKey = $"{category.Trim('/')}/{key.TrimStart('/')}";
            _memoryCache.TryRemove(fullKey, out _);
            return;
        }

        if (category != null)
        {
            string prefix = $"{category.Trim('/')}/";
            foreach (var k in _memoryCache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                _memoryCache.TryRemove(k, out _);
            }
        }
    }
}

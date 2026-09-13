using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Federation;

public sealed class DatasetMemoryCache
{
    private static readonly Lazy<DatasetMemoryCache> _default = new(() => new DatasetMemoryCache());
    public static DatasetMemoryCache Default => _default.Value;

    private readonly ConcurrentDictionary<string, (DateTimeOffset Expiry, IReadOnlyList<IDictionary<string, object?>> Data)> _cache = new(StringComparer.Ordinal);

    public async Task<IReadOnlyList<IDictionary<string, object?>>> GetOrCreateAsync(
        string key,
        TimeSpan duration,
        Func<Task<IReadOnlyList<IDictionary<string, object?>>>> factory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(key, out var entry) && entry.Expiry > now)
        {
            return entry.Data;
        }

        var freshData = await factory().ConfigureAwait(false);
        _cache[key] = (now.Add(duration), freshData);
        return freshData;
    }

    public static string GenerateCacheKey(DatasetDefinition dataset, IDictionary<string, object?>? parameters)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        if (dataset.CachePolicy != null && !string.IsNullOrWhiteSpace(dataset.CachePolicy.CacheKey))
        {
            return dataset.CachePolicy.CacheKey;
        }

        var sb = new StringBuilder();
        sb.Append(dataset.Name).Append('|');
        sb.Append(dataset.Type).Append('|');
        sb.Append(dataset.ConnectionRef ?? string.Empty).Append('|');
        sb.Append(dataset.QueryOrUrl ?? string.Empty).Append('|');

        if (parameters != null && parameters.Count > 0)
        {
            var sortedParams = parameters.OrderBy(k => k.Key, StringComparer.Ordinal);
            foreach (var (k, v) in sortedParams)
            {
                sb.Append(k).Append('=').Append(v?.ToString() ?? "NULL").Append(';');
            }
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return $"BPX_CACHE_{dataset.Name}_{Convert.ToHexString(hashBytes)}";
    }

    public void Clear() => _cache.Clear();

    public bool Remove(string key) => _cache.TryRemove(key, out _);

    public int Count
    {
        get
        {
            var now = DateTimeOffset.UtcNow;
            return _cache.Count(kv => kv.Value.Expiry > now);
        }
    }
}

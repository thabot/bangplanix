using System.Collections.Concurrent;
using SkiaSharp;

namespace Bangplanix.Engine.Fonts;

public sealed class LruGlyphCache
{
    private readonly int _capacity;
    private readonly ConcurrentDictionary<string, SKTextBlob> _cache = new();
    private readonly ConcurrentQueue<string> _lruKeys = new();

    public LruGlyphCache(int capacity = 2048)
    {
        _capacity = capacity;
    }

    public bool TryGet(string key, out SKTextBlob? blob)
    {
        return _cache.TryGetValue(key, out blob);
    }

    public void Add(string key, SKTextBlob blob)
    {
        if (_cache.Count >= _capacity && _lruKeys.TryDequeue(out var oldestKey))
        {
            if (_cache.TryRemove(oldestKey, out var oldBlob))
            {
                oldBlob.Dispose();
            }
        }

        _cache[key] = blob;
        _lruKeys.Enqueue(key);
    }

    public void Clear()
    {
        foreach (var blob in _cache.Values)
        {
            blob.Dispose();
        }
        _cache.Clear();
    }
}

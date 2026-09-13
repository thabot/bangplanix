using System;
using System.Threading.Tasks;
using Bangplanix.Core.Caching;
using Xunit;

namespace Bangplanix.Security.Tests;

public class DeterministicCacheTests
{
    [Fact]
    public void BuildCanonicalCacheKeyShouldProduceIdenticalHashRegardlessOfKeyOrderOrWhitespace()
    {
        var json1 = """
        {
          "version": "1.0.0",
          "title": "Invoice",
          "author": "Acme"
        }
        """;

        var json2 = """
        {"author":"Acme","title":"Invoice","version":"1.0.0"}
        """;

        var key1 = DeterministicCacheKeyBuilder.BuildCanonicalCacheKey(json1);
        var key2 = DeterministicCacheKeyBuilder.BuildCanonicalCacheKey(json2);

        Assert.Equal(key1, key2);
        Assert.Equal(64, key1.Length); // SHA-256 Hex string length
    }

    [Fact]
    public void BuildCanonicalCacheKeyShouldDifferentiateByTenantAndParameters()
    {
        var template = """{"title": "Invoice", "version": "1.0.0"}""";
        var param1 = """{"Customer": "A"}""";
        var param2 = """{"Customer": "B"}""";

        var keyA = DeterministicCacheKeyBuilder.BuildCanonicalCacheKey(template, param1, "tenant-1");
        var keyB = DeterministicCacheKeyBuilder.BuildCanonicalCacheKey(template, param2, "tenant-1");
        var keyTenant2 = DeterministicCacheKeyBuilder.BuildCanonicalCacheKey(template, param1, "tenant-2");

        Assert.NotEqual(keyA, keyB);
        Assert.NotEqual(keyA, keyTenant2);
    }

    [Fact]
    public async Task TwoTierReportCacheShouldServeFromL1AndL2Correctly()
    {
        var l2 = new InMemoryDistributedFallback();
        var cache = new TwoTierReportCache(l2, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

        var key = "report-hash-12345";
        var data = "%PDF-1.4 Data Payload"u8.ToArray();

        // 1. Initial Miss
        var missed = await cache.GetAsync(key);
        Assert.Null(missed);
        Assert.Equal(1, cache.MissCount);

        // 2. Set Cache
        await cache.SetAsync(key, data);

        // 3. L1 Hit
        var hit1 = await cache.GetAsync(key);
        Assert.NotNull(hit1);
        Assert.Equal(1, cache.L1HitCount);

        // 4. Invalidate and re-read from L2
        await cache.InvalidateAsync(key);
        // Put back to L2 only
        await l2.SetAsync(key, data, TimeSpan.FromMinutes(5));

        var l2Hit = await cache.GetAsync(key);
        Assert.NotNull(l2Hit);
        Assert.Equal(1, cache.L2HitCount);
    }
}

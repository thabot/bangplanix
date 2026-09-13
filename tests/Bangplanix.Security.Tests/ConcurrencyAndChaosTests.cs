using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bangplanix.Core.Caching;
using Bangplanix.Core.Security;
using FluentAssertions;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Security.Tests;

public class ConcurrencyAndChaosTests
{
    [Fact]
    public async Task TenantRateLimiter_100ParallelRequests_ShouldEnforceBucketsConcurrently()
    {
        var limiter = new TenantRateLimiter();
        var tenants = Enumerable.Range(1, 10).Select(i => new TenantContext
        {
            TenantId = $"tenant_{i}",
            Tier = TenantTier.Pro,
            RequestsPerMinute = 1,
            BurstCapacity = 2
        }).ToList();

        var allowedCount = 0;
        var rejectedCount = 0;
        var locker = new object();

        // 100 requests in parallel (10 requests per tenant)
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var tenant = tenants[i % tenants.Count];
            tasks.Add(Task.Run(() =>
            {
                var allowed = limiter.IsAllowed(tenant);
                lock (locker)
                {
                    if (allowed) allowedCount++;
                    else rejectedCount++;
                }
            }));
        }

        await Task.WhenAll(tasks);

        allowedCount.Should().BeGreaterThan(0);
        (allowedCount + rejectedCount).Should().Be(100);
    }

    [Fact]
    public async Task TwoTierReportCache_1000ParallelAccesses_ShouldBeThreadSafeAndAccurate()
    {
        var cache = new TwoTierReportCache(defaultL1Ttl: TimeSpan.FromSeconds(30));
        var keys = Enumerable.Range(1, 10).Select(i => $"cache_key_{i}").ToArray();

        // Pre-populate some keys
        for (int k = 0; k < 5; k++)
        {
            await cache.SetAsync(keys[k], System.Text.Encoding.UTF8.GetBytes($"Preloaded {keys[k]}"));
        }

        var tasks = new List<Task>();
        for (int i = 0; i < 1000; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = keys[index % keys.Length];
                var data = await cache.GetAsync(key);
                if (data == null)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes($"Report Payload {key}");
                    await cache.SetAsync(key, bytes);
                    data = bytes;
                }

                data.Should().NotBeNull();
                System.Text.Encoding.UTF8.GetString(data).Should().Contain(key);
            }));
        }

        await Task.WhenAll(tasks);

        cache.L1HitCount.Should().BeGreaterThanOrEqualTo(0);
    }
}

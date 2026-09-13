using System;
using System.Collections.Concurrent;

namespace Bangplanix.Core.Security;

public class TokenBucket
{
    private readonly double _capacity;
    private readonly double _refillRatePerSecond;
    private double _tokens;
    private DateTime _lastRefillUtc;
    private readonly object _lock = new();

    public TokenBucket(double capacity, double refillRatePerSecond)
    {
        _capacity = capacity;
        _refillRatePerSecond = refillRatePerSecond;
        _tokens = capacity;
        _lastRefillUtc = DateTime.UtcNow;
    }

    public bool TryConsume(double count = 1.0)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefillUtc).TotalSeconds;
            _tokens = Math.Min(_capacity, _tokens + (elapsed * _refillRatePerSecond));
            _lastRefillUtc = now;

            if (_tokens >= count)
            {
                _tokens -= count;
                return true;
            }

            return false;
        }
    }
}

public class TenantRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new(StringComparer.Ordinal);

    public bool IsAllowed(TenantContext tenant, int tokens = 1)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (!tenant.IsActive) return false;
        if (tenant.Tier == TenantTier.Internal) return true; // Internal unlimited

        var bucket = _buckets.GetOrAdd(tenant.TenantId, _ =>
        {
            var refillRate = (double)tenant.RequestsPerMinute / 60.0;
            var capacity = tenant.RequestsPerMinute + tenant.BurstCapacity;
            return new TokenBucket(capacity, refillRate);
        });

        return bucket.TryConsume(tokens);
    }
}

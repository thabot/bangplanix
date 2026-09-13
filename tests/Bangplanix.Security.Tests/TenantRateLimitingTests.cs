using System.Collections.Generic;
using Bangplanix.Core.Security;
using Xunit;

namespace Bangplanix.Security.Tests;

public class TenantRateLimitingTests
{
    [Fact]
    public void TenantRateLimiterShouldEnforceRateLimitsAndBurstCapacity()
    {
        var limiter = new TenantRateLimiter();
        var tenant = new TenantContext
        {
            TenantId = "tenant-retail-01",
            RequestsPerMinute = 2,
            BurstCapacity = 1,
            Tier = TenantTier.Pro
        };

        // Capacity = 3 tokens (2 RPM + 1 Burst)
        Assert.True(limiter.IsAllowed(tenant)); // token 1
        Assert.True(limiter.IsAllowed(tenant)); // token 2
        Assert.True(limiter.IsAllowed(tenant)); // token 3
        Assert.False(limiter.IsAllowed(tenant)); // token 4 -> Rate limited!
    }

    [Fact]
    public void InternalTenantShouldAlwaysBypassRateLimiting()
    {
        var limiter = new TenantRateLimiter();
        var internalTenant = new TenantContext
        {
            TenantId = "tenant-internal",
            Tier = TenantTier.Internal
        };

        for (int i = 0; i < 50; i++)
        {
            Assert.True(limiter.IsAllowed(internalTenant));
        }
    }

    [Fact]
    public void IpFilterGuardShouldValidateSingleIpAndCidrSubnets()
    {
        var allowedSubnets = new List<string>
        {
            "192.168.1.0/24",
            "10.0.0.50"
        };

        Assert.True(IpFilterGuard.IsIpAllowed("192.168.1.100", allowedSubnets));
        Assert.True(IpFilterGuard.IsIpAllowed("10.0.0.50", allowedSubnets));
        Assert.False(IpFilterGuard.IsIpAllowed("10.0.0.51", allowedSubnets));
        Assert.False(IpFilterGuard.IsIpAllowed("172.16.0.1", allowedSubnets));
    }

    [Fact]
    public void IpFilterGuardWithEmptyListShouldAllowAll()
    {
        Assert.True(IpFilterGuard.IsIpAllowed("203.0.113.195", []));
    }
}

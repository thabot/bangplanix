using System.Collections.Generic;

namespace Bangplanix.Core.Security;

public enum TenantTier
{
    Free,
    Pro,
    Enterprise,
    Internal
}

public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TenantTier Tier { get; set; } = TenantTier.Pro;
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstCapacity { get; set; } = 10;
    public List<string> AllowedCidrSubnets { get; } = new();
    public bool IsActive { get; set; } = true;
}

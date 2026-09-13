using System;
using System.Collections.Generic;
using System.Net;

namespace Bangplanix.Core.Security;

public static class IpFilterGuard
{
    public static bool IsIpAllowed(string clientIpString, IEnumerable<string> allowedCidrSubnets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIpString);
        ArgumentNullException.ThrowIfNull(allowedCidrSubnets);

        if (!IPAddress.TryParse(clientIpString, out var clientIp))
        {
            return false;
        }

        var anyRules = false;
        foreach (var rule in allowedCidrSubnets)
        {
            if (string.IsNullOrWhiteSpace(rule)) continue;
            anyRules = true;

            if (rule.Contains('/', StringComparison.Ordinal))
            {
                if (IPNetwork.TryParse(rule, out var network) && network.Contains(clientIp))
                {
                    return true;
                }
            }
            else
            {
                if (IPAddress.TryParse(rule, out var singleIp) && singleIp.Equals(clientIp))
                {
                    return true;
                }
            }
        }

        // If no rules defined, allow by default
        return !anyRules;
    }
}

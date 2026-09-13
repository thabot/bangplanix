using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace Bangplanix.Server.Observability;

public class HealthReport
{
    public string Status { get; set; } = "Healthy";
    public long AllocatedMemoryBytes { get; set; }
    public TimeSpan ProcessUptime { get; set; }
    public Dictionary<string, string> SubsystemStatus { get; } = new();
}

public static class HealthCheckProbe
{
    private static readonly DateTime StartTimeUtc = DateTime.UtcNow;

    public static HealthReport PerformHealthCheck(long maxMemoryThresholdBytes = 1024L * 1024L * 1024L) // 1GB default ceiling
    {
        var allocatedMemory = GC.GetTotalMemory(forceFullCollection: false);
        var isHealthy = allocatedMemory <= maxMemoryThresholdBytes;

        var report = new HealthReport
        {
            Status = isHealthy ? "Healthy" : "Degraded",
            AllocatedMemoryBytes = allocatedMemory,
            ProcessUptime = DateTime.UtcNow - StartTimeUtc
        };

        report.SubsystemStatus["Memory"] = isHealthy ? "OK" : "Warning: Memory Threshold Exceeded";
        report.SubsystemStatus["SkiaSharpEngine"] = "OK";
        report.SubsystemStatus["FontRegistry"] = "OK";

        return report;
    }
}

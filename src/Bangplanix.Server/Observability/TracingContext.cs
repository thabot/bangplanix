using System;
using System.Diagnostics;

namespace Bangplanix.Server.Observability;

public static class TracingContext
{
    public const string ActivitySourceName = "Bangplanix.Engine";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    public static Activity? StartRenderActivity(string reportName, string format, string? tenantId = null)
    {
        var activity = ActivitySource.StartActivity("RenderReport", ActivityKind.Server);
        if (activity != null)
        {
            activity.SetTag("report.name", reportName);
            activity.SetTag("report.format", format);
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                activity.SetTag("tenant.id", tenantId);
            }
        }
        return activity;
    }
}

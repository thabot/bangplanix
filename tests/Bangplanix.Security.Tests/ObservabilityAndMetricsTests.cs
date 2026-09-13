using Bangplanix.Server.Observability;
using Xunit;

namespace Bangplanix.Security.Tests;

public class ObservabilityAndMetricsTests
{
    [Fact]
    public void PrometheusMetricsExporterShouldFormatExpositionCorrectly()
    {
        var exporter = new PrometheusMetricsExporter();

        exporter.RecordRenderStart();
        exporter.RecordRenderSuccess(42.5, fromCache: false);

        exporter.RecordRenderStart();
        exporter.RecordRenderSuccess(0.18, fromCache: true);

        exporter.RecordRenderStart();
        exporter.RecordRenderFailure();

        var metrics = exporter.ExportMetrics();

        Assert.Contains("bangplanix_render_total{status=\"success\"} 2", metrics, System.StringComparison.Ordinal);
        Assert.Contains("bangplanix_render_total{status=\"failure\"} 1", metrics, System.StringComparison.Ordinal);
        Assert.Contains("bangplanix_cache_hits_total 1", metrics, System.StringComparison.Ordinal);
        Assert.Contains("bangplanix_cache_misses_total 1", metrics, System.StringComparison.Ordinal);
        Assert.Contains("bangplanix_active_renders 0", metrics, System.StringComparison.Ordinal);
    }

    [Fact]
    public void HealthCheckProbeShouldReturnHealthyStateUnderNormalMemory()
    {
        var report = HealthCheckProbe.PerformHealthCheck();

        Assert.Equal("Healthy", report.Status);
        Assert.True(report.AllocatedMemoryBytes > 0);
        Assert.Equal("OK", report.SubsystemStatus["Memory"]);
        Assert.Equal("OK", report.SubsystemStatus["SkiaSharpEngine"]);
    }

    [Fact]
    public void TracingContextShouldCreateActivityWithProperTags()
    {
        using var activity = TracingContext.StartRenderActivity("SalesInvoice", "pdf", "tenant-100");
        if (activity != null)
        {
            Assert.Equal("RenderReport", activity.OperationName);
        }
    }
}

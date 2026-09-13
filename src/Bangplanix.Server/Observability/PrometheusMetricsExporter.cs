using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Bangplanix.Server.Observability;

public class PrometheusMetricsExporter
{
    private long _totalRenderCount;
    private long _successfulRenderCount;
    private long _failedRenderCount;
    private long _cacheHits;
    private long _cacheMisses;
    private long _activeRenders;
    private double _totalRenderDurationMs;

    public void RecordRenderStart()
    {
        Interlocked.Increment(ref _totalRenderCount);
        Interlocked.Increment(ref _activeRenders);
    }

    public void RecordRenderSuccess(double durationMs, bool fromCache = false)
    {
        Interlocked.Decrement(ref _activeRenders);
        Interlocked.Increment(ref _successfulRenderCount);
        if (fromCache)
        {
            Interlocked.Increment(ref _cacheHits);
        }
        else
        {
            Interlocked.Increment(ref _cacheMisses);
        }

        // Add duration
        double initial, computed;
        do
        {
            initial = _totalRenderDurationMs;
            computed = initial + durationMs;
        } while (Math.Abs(Interlocked.CompareExchange(ref _totalRenderDurationMs, computed, initial) - initial) > 0.0001);
    }

    public void RecordRenderFailure()
    {
        Interlocked.Decrement(ref _activeRenders);
        Interlocked.Increment(ref _failedRenderCount);
    }

    public string ExportMetrics()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# HELP bangplanix_render_total Total number of report render requests");
        sb.AppendLine("# TYPE bangplanix_render_total counter");
        sb.Append("bangplanix_render_total{status=\"success\"} ").Append(_successfulRenderCount).AppendLine();
        sb.Append("bangplanix_render_total{status=\"failure\"} ").Append(_failedRenderCount).AppendLine();

        sb.AppendLine("# HELP bangplanix_cache_hits_total Total number of cache hits");
        sb.AppendLine("# TYPE bangplanix_cache_hits_total counter");
        sb.Append("bangplanix_cache_hits_total ").Append(_cacheHits).AppendLine();

        sb.AppendLine("# HELP bangplanix_cache_misses_total Total number of cache misses");
        sb.AppendLine("# TYPE bangplanix_cache_misses_total counter");
        sb.Append("bangplanix_cache_misses_total ").Append(_cacheMisses).AppendLine();

        sb.AppendLine("# HELP bangplanix_active_renders Number of reports currently rendering");
        sb.AppendLine("# TYPE bangplanix_active_renders gauge");
        sb.Append("bangplanix_active_renders ").Append(_activeRenders).AppendLine();

        sb.AppendLine("# HELP bangplanix_render_duration_ms_total Total duration of all report renders in milliseconds");
        sb.AppendLine("# TYPE bangplanix_render_duration_ms_total counter");
        sb.Append("bangplanix_render_duration_ms_total ").Append(_totalRenderDurationMs.ToString("F2", CultureInfo.InvariantCulture)).AppendLine();

        return sb.ToString();
    }
}
